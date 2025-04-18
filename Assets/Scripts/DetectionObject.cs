public class DetectionObject
{
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
    public float Score { get; set; }
    public int ClassId { get; set; }
    public string[] Labels { get; set; }
}
