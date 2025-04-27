namespace Servicio2.Utility
{
    public class FolioHelper
    {
        public static string GenerarFolio(int id)
        {
            string fecha = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string randomChars = new string(Enumerable.Repeat(chars, 2)
       .Select(s => s[random.Next(s.Length)]).ToArray());

            string idPart = id.ToString();
            if (idPart.Length > 2)
                idPart = idPart.Substring(idPart.Length - 2);

            return $"{fecha}{randomChars}{idPart}";
        }

    }
}
