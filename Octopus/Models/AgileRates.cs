namespace BlazorBattControl.Octopus.Models;

public class AgileRates
{
    public int count { get; set; }
    public object next { get; set; }
    public object previous { get; set; }
    public CostItem[] results { get; set; }
}

public class CostItem
{
    public float value_exc_vat { get; set; }
    public float value_inc_vat { get; set; }
    public DateTime valid_from { get; set; }
    public DateTime valid_to { get; set; }
}
