using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Media;
using Android.Util;
using Stream = Android.Media.Stream;
using IOException = Java.IO.IOException;
using Android;
using Com.Huawei.Hms.Nearby.Transfer;
using Com.Huawei.Hms.Nearby.Discovery;
using System.Collections.Generic;
using Com.Huawei.Agconnect.Config;
using Android.Content;
using Com.Huawei.Hms.Nearby;
using System.Linq;
using DataType = Com.Huawei.Hms.Nearby.Transfer.DataType;
using Com.Huawei.Hms.Common;
using Com.Huawei.Hmf.Tasks;
using Android.Views;
using Android.Graphics.Drawables;
using Android.Content.Res;
using Android.Graphics;

namespace XWalkietalkie
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        public static string TAG = "WalkiTalki";

        // For recording audio as the user speaks.
        public AudioRecorder Recorder;

        // For playing audio from other users nearby.
        public AudioPlayer Player;

        // The phone's original media volume.
        private int OriginalVolume;

        // Micophone button
        private Button record;

        // The State of the Device
        public static State MyState = State.UNKNOWN;

        //DisoveryEngine instance that will be used to Broadcast or scan the nearby devices
        public static DiscoveryEngine MyDiscoveryEngine = null;

        //TransferEngine instance that will be used to send and recevie audio streams
        public static TransferEngine MyTransferEngine = null;

        //Service Id to identify the application
        public static string MyServiceID = "MyServiceID";

        //The Name that will be displayed to others
        public static string MyEndPoint = Build.Model;

        // The devices we've discovered near us. 
        public IDictionary<string, Device> DiscoveredEndpoints = new Dictionary<string, Device>();

        //The devices we have pending connections to. They will stay pending until we 
        //accept  or reject the Connection.
        public IDictionary<string, Device> PendingConnections = new Dictionary<string, Device>();

        //The devices we are currently connected to.
        public IDictionary<string, Device> EstablishedConnections = new Dictionary<string, Device>();

        //True if we are asking a discovered device to connect to us. While we ask, we cannot ask another device.
        public bool IsConnecting = false;

        //True if we are Broadcasting.
        public bool IsBroadcasting = false;

        //True if we are scanning.
        public bool IsScanning = false;

        //Inctance of the current MainActiviy
        private static volatile MainActivity M_SERVICE = null;

        //Displays the current state.
        private TextView mCurrentStateView;

        //Displays the current connected devices.
        private TextView Name;

        //Enum class that represents the States of a device
        public enum State
        {
            UNKNOWN,
            SEARCHING,
            CONNECTED
        }

        protected override void AttachBaseContext(Context context)
        {
            base.AttachBaseContext(context);
            AGConnectServicesConfig config = AGConnectServicesConfig.FromContext(context);
            config.OverlayWith(new HmsLazyInputStream(context));
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
       
            SetContentView(Resource.Layout.activity_main);
          
            record = FindViewById<Button>(Resource.Id.record_button);
            MyTransferEngine = Nearby.GetTransferEngine(this);
            MyDiscoveryEngine = Nearby.GetDiscoveryEngine(this);

            M_SERVICE = this;

            mCurrentStateView = FindViewById<TextView>(Resource.Id.current_state);

            Name = FindViewById<TextView>(Resource.Id.name);

            Name.Text = MyEndPoint;
            SetColor(Color.DimGray);
            record.Enabled = false;

            record.Touch += delegate (object sender, View.TouchEventArgs touchEventArgs)
            {
                switch (touchEventArgs.Event.Action & MotionEventActions.Mask)
                {

                    case MotionEventActions.Down:
                        StartRecording();
                        SetColor(Color.DarkSalmon);
                        break;
                    case MotionEventActions.Up:
                    case MotionEventActions.Outside:
                    case MotionEventActions.Cancel:
                        StopRecording();
                        SetColor(Color.Salmon);
                        break;
                }
            };
            RequestPermissions();
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == Constants.REQ_CODE)
            {
                bool isAllGranted = true;
                foreach (string result in permissions)
                {
                    if (this.CheckSelfPermission(result) == Android.Content.PM.Permission.Denied)
                    {
                        isAllGranted = false;
                        break;
                    }
                }
                if (isAllGranted)
                {
                    Log.Info(TAG, "All permissions are granted");
                }
                else
                {
                    Toast.MakeText(this, "Cannot start without required permissions", ToastLength.Long).Show();
                }
            }
        }
        public string[] GetPermissions()
        {
            return new string[]{
                 Manifest.Permission.Bluetooth,
                 Manifest.Permission.BluetoothAdmin,
                 Manifest.Permission.AccessWifiState,
                 Manifest.Permission.ChangeWifiState,
                 Manifest.Permission.AccessCoarseLocation,
                 Manifest.Permission.AccessFineLocation,
                 Manifest.Permission.ReadExternalStorage,
                 Manifest.Permission.WriteExternalStorage,
                 Manifest.Permission.RecordAudio
        };
        }
        private void RequestPermissions()
        {
            string[] deniedPermissions = PermissionUtil.GetDeniedPermissions(this, GetPermissions());

            if (deniedPermissions != null && deniedPermissions.Length > 0)
            {
                PermissionUtil.RequestPermissions(this, deniedPermissions, Constants.REQ_CODE);
            }
            else
            {
                Log.Info(TAG, "All permissions are granted");
            }
        }
        //Show Warnning Dialog if Bluetooth and location are disabled
        private void ShowWarnDialog(string content)
        {
            Android.App.AlertDialog.Builder builder = new Android.App.AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.warn);
            builder.SetMessage(content);
            builder.SetNegativeButton(Resource.String.btn_confirm, (senderAlert, args) =>
            {
                Process.KillProcess(Process.MyPid());
            });
            Dialog dialog = builder.Create();
            dialog.Show();
        }
        protected override void OnStart()
        {
            base.OnStart();

            // Set the media volume to max.
            VolumeControlStream = Stream.Music;
            AudioManager audioManager = (AudioManager)GetSystemService(AudioService);
            OriginalVolume = audioManager.GetStreamVolume(Stream.Music);
            audioManager.SetStreamVolume(Stream.Music, audioManager.GetStreamMaxVolume(Stream.Music), 0);

            //Check if Bluetooth and Location are enabled
            Log.Info(TAG, "Is support BLE advertiser: " + BluetoothCheckUtil.IsSupportBleAdv());
            Log.Info(TAG, "Is support BLE Scanner: " + BluetoothCheckUtil.IsSupportBleScan());
            base.OnStart();
            if (!BluetoothCheckUtil.IsBlueEnabled())
            {
                ShowWarnDialog(Constants.BLUETOOTH_ERROR);
                return;
            }

            if (!LocationCheckUtil.IsLocationEnabled(this))
            {
                ShowWarnDialog(Constants.LOCATION_SWITCH_ERROR);
                return;
            }
            SetState(State.SEARCHING);

        }

        protected override void OnStop()
        {
            // Restore the original volume.
            AudioManager audioManager = (AudioManager)GetSystemService(AudioService);
            audioManager.SetStreamVolume(Stream.Music, OriginalVolume, 0);
            VolumeControlStream = Stream.Ring;

            // Stop all audio-related threads
            if (IsRecording())
            {
                StopRecording();
            }
            if (IsPlaying())
            {
                StopPlaying();
            }

            // After our Activity stops, we disconnect from Nearby Connections.
            SetState(State.UNKNOWN);

            base.OnStop();
        }
        //Start recording from microphone.
        private void StartRecording()
        {

            try
            {
                ParcelFileDescriptor[] payloadPipe = ParcelFileDescriptor.CreatePipe();

                // Send the first half of the payload (the read side) to Nearby Connections.
                Send(Data.FromStream(payloadPipe[0]));

                // Use the second half of the payload (the write side) in AudioRecorder.
                Recorder = new AudioRecorder(payloadPipe[1], this);
                Recorder.Start();
            }
            catch (IOException e)
            {
                Log.Error(TAG, "StartRecording() failed" + e);
            }
        }

        //Stops streaming sound from the microphone.
        private void StopRecording()
        {

            if (Recorder != null)
            {
                Recorder.Stop();
                Recorder = null;
            }
        }

        //Return True if currently streaming from the microphone.
        private bool IsRecording()
        {
            return Recorder != null && Recorder.IsRecording();
        }

        //Stops all currently streaming audio tracks.
        private void StopPlaying()
        {

            if (Player != null)
            {
                Player.Stop();
                Player = null;
            }
        }

        //Return True if currently playing.
        private bool IsPlaying()
        {
            return Player != null;
        }

        //Called when a pending connection with a remote endpoint is created.
        public void ConnectionInitiated(Device device)
        {
            MyDiscoveryEngine.AcceptConnect(device.EndPoint, new DataCallBackImp(M_SERVICE)).AddOnFailureListener(new TaskListener(M_SERVICE, "AccptingConnection")); ;
        }


        //Called when a connection with this endpoint has failed.
        public void ConnectionFailed()
        {
            if (MyState == State.SEARCHING)
            {
                StartBroadcasting();
            }
        }

        
        //Called when someone has disconnected.
        public void EndpointDisconnected(Device endpoint)
        {
            EstablishedConnections.Remove(endpoint.EndPoint);
            Toast.MakeText(M_SERVICE, " Disconnected from  " + endpoint.EndPoint, ToastLength.Short).Show();
            SetState(State.SEARCHING);
        }

        //Called when someone has connected to us.
        public void EndpointConnected(Device endpoint)
        {
            EstablishedConnections.Add(endpoint.EndPoint, endpoint);
            Toast.MakeText(M_SERVICE, " Connected with  " + endpoint.EndPoint, ToastLength.Short).Show();
            SetState(State.CONNECTED);
        }

        //Called when an Endpoint is discovered.
        public void EndpointDiscovered(Device endpoint)
        {
            StopDiscovering();
            ConnectToEndpoint(endpoint);
        }

        //Disconnects from the given endpoint.
        public void Disconnect(Device endpoint)
        {
            MyDiscoveryEngine.Disconnect(endpoint.EndPoint);
            EstablishedConnections.Remove(endpoint.EndPoint);
        }

        //Disconnects from all currently connected endpoints.
        public void DisconnectFromAllEndpoints()
        {
            MyDiscoveryEngine.DisconnectAll();
            EstablishedConnections.Clear();
        }

        //Resets and clears all state in Nearby Connections.
        public void StopAllEndpoints()
        {
            MyDiscoveryEngine.DisconnectAll();
            IsScanning = false;
            IsBroadcasting = false;
            IsConnecting = false;
            DiscoveredEndpoints.Clear();
            PendingConnections.Clear();
            EstablishedConnections.Clear();
        }

        //Called when a data is received
        public void OnReceive(Device endpoint, Data data)
        {
                if (Player != null)
                {
                    Player.Stop();
                    Player = null;
                }

                AudioPlayer player = new AudioPlayer(data.AsStream().AsInputStream());
                Player = player;
                player.Start();
        }

        // Sets the device to Scanning mode. 
        public void StartScaning()
        {
            IsScanning = true;
            ScanOption.Builder discBuilder = new ScanOption.Builder();
            discBuilder.SetPolicy(Policy.PolicyStar);
            MyDiscoveryEngine.StartScan(MyServiceID, new ScanCallBack(M_SERVICE), discBuilder.Build()).AddOnFailureListener(new TaskListener(M_SERVICE, "StartScanning")); ;
        }

        // Stops Scanning.
        public void StopScaning()
        {
            IsScanning = false;
            MyDiscoveryEngine.StopScan();
        }


        // Sets the device to Broadcasting mode. It will now listen for other devices . 
        public void StartBroadcasting()
        {
            IsBroadcasting = true;
            DiscoveredEndpoints.Clear();

            BroadcastOption.Builder advBuilder = new BroadcastOption.Builder();
            advBuilder.SetPolicy(Policy.PolicyStar);
            MyDiscoveryEngine.StartBroadcasting(MyEndPoint, MyServiceID, new ConnCallBack(M_SERVICE), advBuilder.Build()).AddOnFailureListener(new TaskListener(M_SERVICE, "StartBroadcasting")); ;
        }

        // Stops discovery.
        public void StopDiscovering()
        {
            IsBroadcasting = false;
            MyDiscoveryEngine.StopBroadcasting();
        }

        //Initiate connection with a device
        public void ConnectToEndpoint(Device device)
        {

            // Mark ourselves as connecting so we don't connect multiple times
            IsConnecting = true;

            // Ask to connect
            MyDiscoveryEngine.RequestConnect(MyEndPoint, device.EndPoint, new ConnCallBack(M_SERVICE)).AddOnFailureListener(new TaskListener(M_SERVICE, "StartConnecting")); ; ;

        }

        //Internal Send Method
        public void Send(Data payload)
        {
            Send(payload, EstablishedConnections.Keys);
        }

        //Send Audio Stream
        public void Send(Data payload, ICollection<string> endpoints)
        {
            MyTransferEngine.SendData(endpoints.ToList<string>(), payload);
        }

        //Change the State of the device
        public  void SetState(State state)
        {
            if (MyState == state)
            {
                Log.Debug(TAG, "State set to " + state + " but already in that state");
                return;
            }

            Log.Debug(TAG, "State set to " + state);
            State oldState = MyState;
            MyState = state;
            OnStateChanged(oldState, state);
        }

        public void OnStateChanged(State oldState, State newState)
        {

            // Update Nearby Connections to the new state.
            switch (newState)
            {
                case State.SEARCHING:
                    DisconnectFromAllEndpoints();
                    StartBroadcasting();
                    StartScaning();
                    SetColor(Color.DimGray);
                    record.Enabled = false;
                    break;
                case State.CONNECTED:
                    StopDiscovering();
                    StopScaning();
                    SetColor(Color.Salmon);
                    record.Enabled = true;
                    break;
                case State.UNKNOWN:
                    StopAllEndpoints();
                    SetColor(Color.DimGray);
                    record.Enabled = false;
                    break;
                default:
                    break;
            }
            UpdateTextView(mCurrentStateView, newState);
            UpdateConnectedDevices();
        }

        //Update displayed text
        private void UpdateTextView(TextView textView, State state)
        {
                  
            switch (state)
            {
                case State.SEARCHING:
                    textView.Text = GetString(Resource.String.status_searching);
                    break;
                case State.CONNECTED:
                    textView.Text = GetString(Resource.String.status_connected);
                    break;
                default:
                    textView.Text = GetString(Resource.String.status_unknown); 
                    break;
            }
        }

        //Update the Connected devices displayed on Screen
        private void UpdateConnectedDevices()
        {

            switch (MyState)
            {
                case State.SEARCHING:
                    
                    Name.Text = MyEndPoint;
                  
                    break;
                case State.CONNECTED:
                   
                    Name.Append(" with");
                    
                    foreach (KeyValuePair<string, Device> Map in EstablishedConnections)
                    {
                        Name.Append(" "+Map.Value.Name+" ");
                    }
                    
                    break;
                default:
                    Name.Text = MyEndPoint;
                    break;
            }
        }

        //Set The record button's color
        private void SetColor(Color color)
        {
            Drawable tempDrawable = Resources.GetDrawable(Resource.Drawable.CircleButton);
            LayerDrawable bubble = (LayerDrawable) tempDrawable;
            GradientDrawable solidColor = (GradientDrawable)bubble.FindDrawableByLayerId(Resource.Id.circuleBackground);
            solidColor.SetColor(ColorStateList.ValueOf(color));
            record.SetBackgroundDrawable(tempDrawable);
        }
    }

    //Implementation of ScanEndpointCallback class
    class ScanCallBack : ScanEndpointCallback
    {

        private MainActivity context;
        public ScanCallBack(MainActivity Main)
        {
            this.context = Main;
        }
        //Called when a device is found
        public override void OnFound(string EndPoint, ScanEndpointInfo scanEndpointInfo)
        {
            if (MainActivity.MyServiceID.Equals(scanEndpointInfo.ServiceId))
            {
                if (!context.DiscoveredEndpoints.ContainsKey(EndPoint)){
                    Device device = new Device(scanEndpointInfo.Name, EndPoint);
                    context.DiscoveredEndpoints.Add(EndPoint, device);
                    context.EndpointDiscovered(device);
                }
            }

        }

        //Called when a device is lost
        public override void OnLost(string EndPoint)
        {
            Toast.MakeText(context, " One Endpoint Lost " + EndPoint, ToastLength.Long).Show();

        }
    }
    //Implementation of ConnectCallback class
    class ConnCallBack : ConnectCallback
    {
        private MainActivity context;
        public ConnCallBack(MainActivity Main)
        {
            this.context = Main;
        }

        //Called back when the remote endpoint disconnects or the connection is unreachable
        public override void OnDisconnected(string EndPoint)
        {
           
            if (context.EstablishedConnections.ContainsKey(EndPoint))
            {
                context.EndpointDisconnected(context.EstablishedConnections[EndPoint]);
            }
          
        }

        //Called back when a connection has been established and both ends need to confirm whether to accept the connection
        public override void OnEstablish(string EndPoint, ConnectInfo Coninfo)
        {
            Device device = new Device(Coninfo.EndpointName, EndPoint);
            context.PendingConnections.Add(EndPoint, device);
            context.ConnectionInitiated(device);           
        }

        //Called back when either end accepts or rejects the connection.
        public override void OnResult(string EndPoint, ConnectResult ConnResult)
        {
            context.IsConnecting = false;
            if (ConnResult.Status.StatusCode == StatusCode.StatusSuccess)
            {
                Toast.MakeText(context, "Connection with " + EndPoint + " has been Established", ToastLength.Long).Show();

                context.EndpointConnected(context.PendingConnections[EndPoint]);
                if (context.PendingConnections.ContainsKey(EndPoint))
                    context.PendingConnections.Remove(EndPoint);
            }
            else
            {
                context.ConnectionFailed();
                if (context.PendingConnections.ContainsKey(EndPoint))
                    context.PendingConnections.Remove(EndPoint);
                Toast.MakeText(context, "Connection Failed " + ConnResult.Status.StatusMessage, ToastLength.Long).Show();
            }
            
            
        }
    }
    //Implementation of DataCallback class
    class DataCallBackImp : DataCallback
    {
        private MainActivity context;
        public DataCallBackImp(MainActivity Main)
        {
            this.context = Main;
        }

        //Called when a data is received
        public override void OnReceived(string endpoint, Data data)
        {
            Log.Debug(MainActivity.TAG, "OnReceived, Data.Type = " + data.Type);
            Log.Debug(MainActivity.TAG, "OnReceived, string ======== " + endpoint);
            switch (data.Type)
            {
                case DataType.Stream:
                    context.OnReceive(context.EstablishedConnections[endpoint], data);
                    Log.Info(MainActivity.TAG, "nReceived [FilePayload] success, Data.Type.Stream, payloadId ===" + data.Id);
                    break;
                default:
                    return;
            }
        }

        //Called back to obtain the data sending or receiving status.
        public override void OnTransferUpdate(string Endpoint, TransferStateUpdate update)
        {

            long payloadId = update.DataId;
            Log.Debug(MainActivity.TAG, "onTransferUpdate, payloadId============" + payloadId);
            switch (update.Status)
            {
                case TransferStatus.TransferStateSuccess:
                    Log.Debug(MainActivity.TAG, "onTransferUpdate.Status============success.");                      
                    break;
                case TransferStatus.TransferStateInProgress:
                    Log.Debug(MainActivity.TAG, "onTransferUpdate.Status==========transfer in progress.");
                    break;
                case TransferStatus.TransferStateFailure:
                    Log.Debug(MainActivity.TAG, "oOnTransferUpdate.Status==========Failure" );
                    break;
                default:
                    Log.Debug(MainActivity.TAG, "onTransferUpdate.Status=======" + update.Status);
                    return;
            }

        }
    }
    //Implementation of IOnFailureListener interface to catch the result of executing Tasks
    class TaskListener : Java.Lang.Object,  IOnFailureListener
    {

        private MainActivity context;
        string Caller;
        public TaskListener(MainActivity context, string caller)
        {
            this.context = context;
            this.Caller = caller;

        }

        //Called when the execution of a task is failed
        public void OnFailure(Java.Lang.Exception e)
        {
            Log.Error(MainActivity.TAG, Caller + " OnFailure");
            ApiException Excp = (ApiException)e;
            Toast.MakeText(context, Caller + " Failed " + Excp.StatusCode + " " + Excp.StatusMessage, ToastLength.Long).Show();
            switch (Caller)
            {
                case "StartScanning":
                    context.IsScanning = false;
                    break;
                case "StartBroadcasting":
                    context.IsBroadcasting = false;
                    break;
                case "StartConnecting":
                    context.IsConnecting = false;
                    context.ConnectionFailed();
                    break;
                default:
                    return;
            }

        }
    }
}