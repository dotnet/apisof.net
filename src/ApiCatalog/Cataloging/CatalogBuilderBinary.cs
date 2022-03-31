using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ApiCatalog
{
    // TODO: We should probably delete CatalogBuilderBinary and merge CatalogBuilder with CatalogBuilderSQLite
    public sealed class CatalogBuilderBinary : CatalogBuilder
    {
        internal const short Version = 0;
        private readonly Table<string> _stringTable = Table<string>.Create(StringComparer.Ordinal);
        private readonly Table<Guid> _apiTable = Table<Guid>.Create();
        private readonly Table<Guid> _assemblyTable = Table<Guid>.Create();
        private readonly Table<Guid> _packageTable = Table<Guid>.Create();
        private readonly Table _declarationTable = Table.Create();
        private readonly Table _frameworkTable = Table.Create();
        private readonly Table _frameworkAssemblyTable = Table.Create();
        private readonly Table _packageAssemblyTable = Table.Create();

        public void WriteTo(Stream stream)
        {
            const int tableCount = 8;

            using (var binaryWriter = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                var origin = stream.Position;

                binaryWriter.Write((byte)'A');
                binaryWriter.Write((byte)'P');
                binaryWriter.Write((byte)'I');
                binaryWriter.Write((byte)'C');
                binaryWriter.Write(Version);
                binaryWriter.Write(tableCount);
                binaryWriter.Write(_stringTable.Size);
                binaryWriter.Write(_apiTable.Size);
                binaryWriter.Write(_assemblyTable.Size);
                binaryWriter.Write(_packageTable.Size);
                binaryWriter.Write(_declarationTable.Size);
                binaryWriter.Write(_frameworkTable.Size);
                binaryWriter.Write(_frameworkAssemblyTable.Size);
                binaryWriter.Write(_packageAssemblyTable.Size);
            }

            _stringTable.WriteTo(stream);
            _apiTable.WriteTo(stream);
            _assemblyTable.WriteTo(stream);
            _packageTable.WriteTo(stream);
            _declarationTable.WriteTo(stream);
            _frameworkTable.WriteTo(stream);
            _frameworkAssemblyTable.WriteTo(stream);
            _packageAssemblyTable.WriteTo(stream);
        }

        private int DefineString(string value)
        {
            if (_stringTable.TryGetRowIndex(value, out var rowIndex))
                return rowIndex;

            rowIndex = _stringTable.BeginRow(value);
            _stringTable.WriteString(value);
            return rowIndex;
        }

        protected override void DefineApi(Guid fingerprint, ApiKind kind, Guid parentFingerprint, string name)
        {
            if (_apiTable.Contains(fingerprint))
                return;

            var parentRowIndex = parentFingerprint == Guid.Empty ? -1 : _apiTable.GetRowIndex(parentFingerprint);
            var nameRowIndex = DefineString(name);

            _apiTable.BeginRow(fingerprint);
            _apiTable.WriteGuid(fingerprint);
            _apiTable.WriteByte((byte)kind);
            _apiTable.WriteInt32(parentRowIndex);
            _apiTable.WriteInt32(nameRowIndex);
        }

        protected override bool DefineAssembly(Guid fingerprint, string name, string version, string publicKeyToken)
        {
            if (_assemblyTable.Contains(fingerprint))
                return false;

            var nameRowIndex = DefineString(name);
            var versionRowIndex = DefineString(version);
            var publicKeyTokenRowIndex = DefineString(publicKeyToken);

            _assemblyTable.BeginRow(fingerprint);
            _assemblyTable.WriteInt32(nameRowIndex);
            _assemblyTable.WriteInt32(versionRowIndex);
            _assemblyTable.WriteInt32(publicKeyTokenRowIndex);
            return true;
        }

        protected override void DefineDeclaration(Guid assemblyFingerprint, Guid apiFingerprint, string syntax)
        {
            var assemblyRowIndex = _assemblyTable.GetRowIndex(assemblyFingerprint);
            var apiRowIndex = _apiTable.GetRowIndex(apiFingerprint);
            var syntaxRowIndex = DefineString(syntax);

            _declarationTable.BeginRow();
            _declarationTable.WriteInt32(assemblyRowIndex);
            _declarationTable.WriteInt32(apiRowIndex);
            _declarationTable.WriteInt32(syntaxRowIndex);
        }

        protected override void DefineFramework(string frameworkName)
        {
            var nameRowIndex = DefineString(frameworkName);

            _frameworkTable.BeginRow();
            _frameworkTable.WriteInt32(nameRowIndex);
        }

        protected override void DefineFrameworkAssembly(string framework, Guid assemblyFingerprint)
        {
            var nameRowIndex = _stringTable.GetRowIndex(framework);
            var assemblyRowIndex = _assemblyTable.GetRowIndex(assemblyFingerprint);

            _frameworkAssemblyTable.BeginRow();
            _frameworkAssemblyTable.WriteInt32(nameRowIndex);
            _frameworkAssemblyTable.WriteInt32(assemblyRowIndex);
        }

        protected override void DefinePackage(Guid fingerprint, string id, string version)
        {
            if (_packageTable.Contains(fingerprint))
                return;

            var idRowIndex = DefineString(id);
            var versionRowIndex = DefineString(version);

            _packageTable.BeginRow(fingerprint);
            _packageTable.WriteInt32(idRowIndex);
            _packageTable.WriteInt32(versionRowIndex);
        }

        protected override void DefinePackageAssembly(Guid packageFingerprint, string framework, Guid assemblyFingerprint)
        {
            var packageRowIndex = _packageTable.GetRowIndex(packageFingerprint);
            var frameworkRowIndex = _stringTable.GetRowIndex(framework);
            var assemblyRowIndex = _assemblyTable.GetRowIndex(assemblyFingerprint);

            _packageAssemblyTable.BeginRow();
            _packageAssemblyTable.WriteInt32(packageRowIndex);
            _packageAssemblyTable.WriteInt32(frameworkRowIndex);
            _packageAssemblyTable.WriteInt32(assemblyRowIndex);
        }

        protected override void DefineUsageSource(string name, DateOnly date)
        {
        }

        protected override void DefineApiUsage(string usageSourceName, Guid apiFingerprint, float percentage)
        {
        }

        private struct Table
        {
            private int _count;
            private MemoryStream _memory;
            private BinaryWriter _writer;

            public int Size => checked((int)_memory.Length);

            public void BeginRow()
            {
                _count++;
            }

            public void WriteInt32(int value)
            {
                _writer.Write(value);
            }

            public void WriteTo(Stream stream)
            {
                using (var binaryWriter = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    binaryWriter.Write(_count);

                _writer.Flush();
                _memory.Position = 0;
                _memory.CopyTo(stream);
            }

            public static Table Create()
            {
                var memory = new MemoryStream();
                return new Table
                {
                    _memory = new MemoryStream(),
                    _writer = new BinaryWriter(memory)
                };
            }
        }

        private readonly struct Table<TKey>
        {
            private readonly Dictionary<TKey, int> _rowIndices;
            private readonly MemoryStream _memory;
            private readonly BinaryWriter _writer;

            public Table(IEqualityComparer<TKey> comparer)
            {
                _rowIndices = new Dictionary<TKey, int>(comparer);
                _memory = new MemoryStream();
                _writer = new BinaryWriter(_memory);
            }

            public int Size => checked((int)_memory.Length);

            public bool Contains(TKey key)
            {
                return _rowIndices.ContainsKey(key);
            }

            public bool TryGetRowIndex(TKey key, out int offset)
            {
                return _rowIndices.TryGetValue(key, out offset);
            }

            public int GetRowIndex(TKey key)
            {
                return _rowIndices[key];
            }

            public int BeginRow(TKey key)
            {
                _writer.Flush();
                var rowIndex = checked((int)_memory.Position);
                _rowIndices.Add(key, rowIndex);
                return rowIndex;
            }

            public void WriteGuid(Guid guid)
            {
                var bytes = (Span<byte>)stackalloc byte[16];
                guid.TryWriteBytes(bytes);
                _writer.Write(bytes);
            }

            public void WriteByte(byte value)
            {
                _writer.Write(value);
            }

            public void WriteInt32(int value)
            {
                _writer.Write(value);
            }

            public void WriteString(string value)
            {
                _writer.Write(value);
            }

            public void WriteTo(Stream stream)
            {
                using (var binaryWriter = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    binaryWriter.Write(_rowIndices.Count);

                _writer.Flush();
                _memory.Position = 0;
                _memory.CopyTo(stream);
            }

            public static Table<TKey> Create() => Create(EqualityComparer<TKey>.Default);
            public static Table<TKey> Create(IEqualityComparer<TKey> comparer) => new Table<TKey>(comparer);
        }
    }
}
