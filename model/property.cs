namespace buyselwebapi.model
{
    public class Property
    {
        public int id { get; set; }
        public string title { get; set; }
        public string address { get; set; }
        public DateTime? dte { get; set; }
        public int sellerid { get; set; }
        public decimal? price { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }

        public string? titlesrchcouncilrateazureblob { get; set; }
        public bool? titlesrchcouncilrateverified { get; set; }
        public string? typeofprop { get; set; }
        public string? suburb { get; set; }
        public string? postcode { get; set; }
        public string? state { get; set; }
        public string? country { get; set; }
        public int? beds { get; set; }
        public int? baths { get; set; }
        public int? carspaces { get; set; }
        public int? landsize { get; set; }
        public int? buildyear { get; set; }
        public string? buildinginspazureblob { get; set; }
        public bool? buildinginspverified { get; set; }

        public string? pestinspazureblob { get; set; }
        public bool? pestinspverified { get; set; }


        public bool? titlesrchcouncilratepublic { get; set; }
        public bool? pestinsppublic { get; set; }
        public bool? buildinginsppublic { get; set; }
        public string? status{ get; set; }
        public string? rejectedreason { get; set; }

        public bool? contractsale { get; set; }
        public bool? poolcert { get; set; }
        public bool? smokealarm { get; set; }
    }
    public class PropertyPic
    {
        public int id { get; set; }
        public string title { get; set; }
        public string address { get; set; }
        public DateTime? dte { get; set; }
        public int sellerid { get; set; }
        public decimal? price { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }
        public string? photobloburl { get; set; }

        public string? titlesrchcouncilrateazureblob { get; set; }
        public bool? titlesrchcouncilrateverified { get; set; }
        public string? typeofprop { get; set; }
        public string? suburb { get; set; }
        public string? postcode { get; set; }
        public int? beds { get; set; }
        public int? baths { get; set; }
        public int? carspaces { get; set; }
        public int? landsize { get; set; }
        public int? buildyear { get; set; }
        public string? state { get; set; }
        public string? country { get; set; }

        public string? buildinginspazureblob { get; set; }
        public bool? buildinginspverified { get; set; }

        public string? pestinspazureblob { get; set; }
        public bool? pestinspverified { get; set; }

        public bool? titlesrchcouncilratepublic { get; set; }
        public bool? pestinsppublic { get; set; }
        public bool? buildinginsppublic { get; set; }
        public string? status { get; set; }
        public string? rejectedreason { get; set; }

        public bool? contractsale { get; set; }
        public bool? poolcert { get; set; }
        public bool? smokealarm { get; set; }
    }
}
