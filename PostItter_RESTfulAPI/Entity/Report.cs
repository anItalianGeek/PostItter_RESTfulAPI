namespace PostItter_RESTfulAPI.Entity;

public class Report
{
    public User reported_by { get; set; }
    public User reportedUser { get; set; }
    public string reason { get; set; }
    public string explanation { get; set; }
}