using Duplicati.Library.Localization.Short;
namespace Duplicati.Library.Backend.ENotariado.Strings {
    internal static class ENotariadoBackend {
        public static string DisplayName { get { return LC.L(@"e-Notariado"); } }
        public static string Description_v2 { get { return LC.L(@"This backend can read and write data to Azure blob storage using the eNotariado system.  Allowed formats are: ""enotariado://containername"""); } }
    }
}
