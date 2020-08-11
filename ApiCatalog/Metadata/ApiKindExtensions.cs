namespace ApiCatalog
{
    public static class ApiKindExtensions
    {
        public static bool IsType(this ApiKind kind)
        {
            switch (kind)
            {
                case ApiKind.Interface:
                case ApiKind.Delegate:
                case ApiKind.Enum:
                case ApiKind.Struct:
                case ApiKind.Class:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsMember(this ApiKind kind)
        {
            switch (kind)
            {
                case ApiKind.Field:
                case ApiKind.EnumItem:
                case ApiKind.Constant:
                case ApiKind.Constructor:
                case ApiKind.Destructor:
                case ApiKind.Operator:
                case ApiKind.Property:
                case ApiKind.Method:
                case ApiKind.Event:
                    return true;
                default:
                    return false;
            }
        }

    }
}
