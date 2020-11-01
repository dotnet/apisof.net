﻿using System.Collections.Generic;

namespace ApiCatalog
{
    /// <summary>
    /// This is used to enumerate the frameworks that should be indexed.
    /// </summary>
    public abstract class FrameworkProvider
    {
        public abstract IEnumerable<(string FrameworkName, FileSet FileSet)> Resolve();
    }
}