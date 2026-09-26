namespace CreditWorks.Web.Models;

public static class VehicleIcons
{
    public static string Symbol(string icon) => icon switch
    {
        "car" => "🚗", "van" => "🚐", "truck" => "🚚", "bus" => "🚌", "motorcycle" => "🏍️", _ => "🚘"
    };
}
