namespace SmartPantry.Permissions;

public static class SmartPantryPermissions
{
    public const string GroupName = "SmartPantry";


    public static class Books
    {
        public const string Default = GroupName + ".Books";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Authors
    {
        public const string Default = GroupName + ".Authors";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>
    /// Administración del catálogo compartido de productos. Consultar, dar de alta y guardar desde
    /// Open Food Facts sólo requiere sesión; editar y eliminar afectan a las despensas de todos los
    /// usuarios, así que requieren estos permisos (por defecto, sólo el rol admin).
    /// </summary>
    public static class Products
    {
        public const string Default = GroupName + ".Products";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    //Add your own permission names. Example:
    //public const string MyPermission1 = GroupName + ".MyPermission1";
}
