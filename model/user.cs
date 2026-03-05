using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buyselwebapi.model
{
    [NotMapped]
    public class Cred
    {
        public string username { get; set; }
        public string? password { get; set; }
    }
    [NotMapped]
    public class tokenadmin
    {
        public string? token { get; set; }
        public bool ok { get; set; }
        public string? username { get; set; }
    }
    public class OAuthUserRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Picture { get; set; }

        [Required]
        public string Provider { get; set; } = string.Empty; // "google", "microsoft", "facebook"

        [Required]
        public string ProviderId { get; set; } = string.Empty;
    }
    public class User
    {
        public int id { get; set; }
        public string email { get; set; }
        public string? firstname { get; set; }
        public string?  lastname { get; set; }
        public string? mobile { get; set; }
        public string? address { get; set; }
        public string? idbloburl { get; set; }
        public DateTime? idverified { get; set; }
        public DateTime? dte { get; set; }

        public bool? termsconditions { get; set; }

        public bool? privacypolicy { get; set; }

        public string? middlename { get; set; }

        public DateTime? dateofbirth { get; set; }

        public string? residencystatus { get; set; }

        public string? maritalstatus { get; set; }

        public string? powerofattorney { get; set; }

        public string? idtype { get; set; }

        public string? ratesnotice { get; set; }

        public DateTime? ratesnoticeverified { get; set; }

        public string? titlesearch { get; set; }

        public DateTime? titlesearchverified { get; set; }
        public bool? admin { get; set; }
        public string? photoazurebloburl { get; set; }
        public bool? photoverified { get; set; }
    }
}
