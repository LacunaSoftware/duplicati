using Duplicati.Library.Localization.Short;
namespace Duplicati.Library.Backend.ENotariado.Strings {
    internal static class ENotariadoBackend {
        public static string DisplayName { get { return LC.L(@"eNotariado"); } }
        public static string NoCertificateThumbprint { get { return LC.L(@"O certificado da aplica��o n�o est� corretamente sincronizado com o eNotariado."); } }
        public static string NoApplicationId { get { return LC.L(@"A aplica��o n�o foi corretamente sincronizada com o eNotariado."); } }
        public static string Description_v2 { get { return LC.L(@"This backend can read and write data to Azure blob storage using the eNotariado system.  Allowed formats are: ""enotariado://containername"""); } }
    }
}
