namespace BlazorBattControl.Octopus.Models;

public class OctopusList
{
    public int count { get; set; }
    public int? next { get; set; }
    public int? previous { get; set; }
}

public class OctopusProducts : OctopusList
{
    public List<ProductDetail>? results { get; set; }
}

public class ProductDetail
{
    public string code { get; set; }
    public string full_name { get; set; }
    public string display_name { get; set; }
    public string description { get; set; }
    public bool is_variable { get; set; }
    public bool is_green { get; set; }
    public bool is_tracker { get; set; }
    public bool is_prepay { get; set; }
    public bool is_business { get; set; }
    public bool is_restricted { get; set; }
    public object term { get; set; }
    public DateTime available_from { get; set; }
    public object available_to { get; set; }
    public DateTime tariffs_active_at { get; set; }
    public Single_Register_Electricity_Tariffs single_register_electricity_tariffs { get; set; }
}

public class Single_Register_Electricity_Tariffs
{
    public _A _A { get; set; }
}

public class _A
{
    public Direct_Debit_Monthly direct_debit_monthly { get; set; }
}

public class Direct_Debit_Monthly
{
    public string code { get; set; }
    public float standing_charge_exc_vat { get; set; }
    public float standing_charge_inc_vat { get; set; }
    public int online_discount_exc_vat { get; set; }
    public int online_discount_inc_vat { get; set; }
    public int dual_fuel_discount_exc_vat { get; set; }
    public int dual_fuel_discount_inc_vat { get; set; }
    public int exit_fees_exc_vat { get; set; }
    public int exit_fees_inc_vat { get; set; }
    public string exit_fees_type { get; set; }
    public Link[] links { get; set; }
    public float standard_unit_rate_exc_vat { get; set; }
    public float standard_unit_rate_inc_vat { get; set; }
}

public class Link
{
    public string href { get; set; }
    public string method { get; set; }
    public string rel { get; set; }
}

