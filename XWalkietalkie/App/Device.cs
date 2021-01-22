namespace XWalkietalkie
{
    public class Device:Java.Lang.Object
    {
        public string Name { set; get; }
        public string EndPoint { set; get; }


        public Device(string name, string endPoint)
        {
            Name = name;
            EndPoint = endPoint;

        }
    }
}