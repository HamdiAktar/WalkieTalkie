using Android.Content;
using Android.Locations;
namespace XWalkietalkie
{
    class LocationCheckUtil
    {
        public static bool IsLocationEnabled(Context context)
        {
           Java.Lang.Object obj = context.GetSystemService(Context.LocationService);
            if (!(obj.GetType()== typeof(LocationManager))) {
                return false;
            }
            LocationManager locationManager = (LocationManager)obj;
            return locationManager.IsProviderEnabled(LocationManager.GpsProvider);
        }
    }
}