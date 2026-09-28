namespace EKR.API.Models;

public enum OrderStatus
{
    Created = 0,
    Accepted = 1,
    SentToFactory = 2,
    InProduction = 3,
    Ready = 4,
    Shipped = 5
}
