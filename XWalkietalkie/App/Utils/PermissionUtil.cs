using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace XWalkietalkie
{
    class PermissionUtil
    {
        public static bool HasPermission(Context context, string permission)
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                if (context.CheckSelfPermission(permission) != Permission.Granted)
                {
                    return false;
                }
            }
            return true;
        }

        public static void RequestPermissions(Activity activity, string[] permissions, int requestCode)
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                activity.RequestPermissions(permissions, requestCode);
            }
        }

        public static string[] GetDeniedPermissions(Context context, string[] permissions)
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                List<string> deniedPermissionList = new List<string>();
                foreach (string permission in permissions)
                {
                    if (context.CheckSelfPermission(permission) != Permission.Granted)
                    {
                        deniedPermissionList.Add(permission);
                    }
                }
                int size = deniedPermissionList.Count;
                if (size > 0)
                {
                    return deniedPermissionList.ToArray();
                }
            }
            return new string[0];
        }
    }
}