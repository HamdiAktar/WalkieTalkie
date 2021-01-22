using Android.Bluetooth;

namespace XWalkietalkie
{
    class BluetoothCheckUtil
    {
        public static bool IsBlueEnabled()
        {
            BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (bluetoothAdapter == null)
            {
                return false;
            }
            if (bluetoothAdapter.Enable())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsSupportBleAdv()
        {
            BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (bluetoothAdapter == null)
            {
                return false;
            }

            if (bluetoothAdapter.BluetoothLeAdvertiser== null)
            {
                return false;
            }

            return true;
        }

        public static bool IsSupportBleScan()
        {
            BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (bluetoothAdapter == null)
            {
                return false;
            }

            if (bluetoothAdapter.BluetoothLeScanner == null)
            {
                return false;
            }

            return true;
        }
    }
}