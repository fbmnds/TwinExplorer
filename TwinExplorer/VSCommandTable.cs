namespace TwinExplorer
{
    using System;
    
    /// <summary>
    /// Helper class that exposes all GUIDs used across VS Package.
    /// </summary>
    internal sealed partial class PackageGuids
    {
        public const string guidTwinExplorerCommandPackageString = "1c2becd7-3f39-4522-936b-dab1c85b7e81";
        public const string guidTwinExplorerCommandPackageCmdSetString = "f4da10d4-695e-44e1-a82e-fc0ed6cac797";
        public const string guidImagesString = "d77d3bcf-a8ba-4eb0-b5ec-cf047551b566";
        public static Guid guidTwinExplorerCommandPackage = new Guid(guidTwinExplorerCommandPackageString);
        public static Guid guidTwinExplorerCommandPackageCmdSet = new Guid(guidTwinExplorerCommandPackageCmdSetString);
        public static Guid guidImages = new Guid(guidImagesString);
    }
    /// <summary>
    /// Helper class that encapsulates all CommandIDs uses across VS Package.
    /// </summary>
    internal sealed partial class PackageIds
    {
        public const int MyMenuGroup = 0x1020;
        public const int TwinExplorerCommandId = 0x0100;
        public const int bmpPic1 = 0x0001;
    }
}
