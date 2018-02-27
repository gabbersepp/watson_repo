using System.Collections.Generic;
using Microsoft.Data.OData;

namespace WebApplication1
{
    public class TranslationRepository : Dictionary<string, string>
    {
        public static TranslationRepository Instance => new TranslationRepository();
        public TranslationRepository()
        {
            this["BothMissing"] =
                "Es fehlen beide Angaben. Bitte teilen Sie uns mit in welcher Firma Sie arbeiten und welchen Accounttyp sie haben?";
            this["UserTypeMissing"] = "Welchen Accounttyp haben Sie";
            this["CompanyMissing"] = "In welcher Firma arbeiten Sie?";
        }
    }
}