namespace BlazorBattControl.Octopus.Models;


public class Products
{
    public int count { get; set; }
    public object next { get; set; }
    public object previous { get; set; }
    public Product[] results { get; set; }
}

public class Product
{
    public string code { get; set; }
    public string direction { get; set; }
    public string full_name { get; set; }
    public string display_name { get; set; }
    public string description { get; set; }
    public bool is_variable { get; set; }
    public bool is_green { get; set; }
    public bool is_tracker { get; set; }
    public bool is_prepay { get; set; }
    public bool is_business { get; set; }
    public bool is_restricted { get; set; }
    public int? term { get; set; }
    public DateTime available_from { get; set; }
    public object available_to { get; set; }
    public PLink[] links { get; set; }
    public string brand { get; set; }
}

public class PLink
{
    public string href { get; set; }
    public string method { get; set; }
    public string rel { get; set; }
}
