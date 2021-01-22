using System;
using System.IO;
using Android.Util;
using Android.Content;
using Com.Huawei.Agconnect.Config;

namespace XWalkietalkie
{
    class HmsLazyInputStream : LazyInputStream
    {
        public HmsLazyInputStream(Context context) : base(context)
        {
        }

        public override Stream Get(Context context)
        {
            try
            {
                return context.Assets.Open("agconnect-services.json");
            }
            catch (Exception e)
            {
                Log.Error(e.ToString(), "Can't open agconnect file");
                return null;
            }
        }
    }
}