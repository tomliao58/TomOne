using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Locations;
using Android.Content.PM;
using Android.Graphics;
using Android.Provider;

namespace TomOne
{
	[Activity (Label = "景點詳細資訊")]			
	public class POIDetailActivity : Activity, ILocationListener
	{
		const int CAPTURE_PHOTO = 0;

		//private declarations
		PointOfInterest _poi;
		LocationManager _locMgr;

		EditText _nameEditText;
		EditText _descrEditText;
		EditText _addrEditText;
		EditText _latEditText;
		EditText _longEditText;

		ImageButton _locationImageButton;
		ImageButton _mapImageButton;
		ImageButton _photoImageButton;
		ImageView _poiImageView;

		ProgressDialog _progressDialog;
		bool _obtainingLocation = false;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create your application here
			SetContentView (Resource.Layout.POIDetail);
			_locMgr = GetSystemService (Context.LocationService) as LocationManager;

			_nameEditText = FindViewById<EditText> (Resource.Id.nameEditText);
			_descrEditText = FindViewById<EditText> (Resource.Id.descrEditText);
			_addrEditText = FindViewById<EditText> (Resource.Id.addrEditText);
			_latEditText = FindViewById<EditText> (Resource.Id.latEditText);
			_longEditText = FindViewById<EditText> (Resource.Id.longEditText);
			_poiImageView = FindViewById<ImageView> (Resource.Id.poiImageView);
			_locationImageButton = FindViewById<ImageButton> (Resource.Id.locationImageButton);
			_mapImageButton = FindViewById<ImageButton> (Resource.Id.mapImageButton);
			_photoImageButton = FindViewById<ImageButton> (Resource.Id.photoImageButton);

			_locationImageButton.Click += GetLocationClicked;
			_addrEditText.FocusChange += GetLocationFromAddress;
			_mapImageButton.Click += MapClicked;
			_photoImageButton.Click += NewPhotoClicked;

			if (Intent.HasExtra ("poiId")) {
				int poiId = Intent.GetIntExtra ("poiId", -1);
				_poi = POIData.Service.GetPOI (poiId);
				Bitmap poiImage = POIData.GetImageFile (_poi.Id.Value);
				_poiImageView.SetImageBitmap (poiImage);
				if (poiImage != null) {
					_poiImageView.Visibility = ViewStates.Visible;
					poiImage.Dispose ();
				}
				//Console.WriteLine ("_poiImageView's visibility is " + _poiImageView.Visibility.ToString());
			} else {
				//Tom: 使用者按了新增鍵
				_poi = new PointOfInterest ();
				//Console.WriteLine ("in else");
				this.Title = "新增景點";

				AlertDialog.Builder alertConfirm = new AlertDialog.Builder (this);
				alertConfirm.SetCancelable (false);
				alertConfirm.SetPositiveButton ("是", GetLocationClicked);
				alertConfirm.SetNegativeButton ("否", delegate {});
				alertConfirm.SetTitle ("自動定位?");
				alertConfirm.SetMessage (String.Format("若欲以自動定位目前所在位置及地址，請按[是]，或按[否]手動輸入地址。"));
				alertConfirm.Show ();
			}

			UpdateUI ();
		}

		protected void UpdateUI()
		{
			_nameEditText.Text = _poi.Name;
			_descrEditText.Text = _poi.Description;
			_addrEditText.Text = _poi.Address;
			_latEditText.Text = _poi.Latitude.ToString ();
			_longEditText.Text = _poi.Longitude.ToString ();
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			MenuInflater.Inflate (Resource.Menu.POIDetailMenu, menu);
			return base.OnCreateOptionsMenu (menu);
		}

		public override bool OnPrepareOptionsMenu(IMenu menu)
		{
			base.OnPrepareOptionsMenu (menu);
			//diable delete for a new POI
			if (!_poi.Id.HasValue) {
				IMenuItem item = menu.FindItem (Resource.Id.actionDelete);
				item.SetEnabled (false);
			}
			return true;
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId) {
			case Resource.Id.actionSave:
				SavePOI ();
				return true;
			case Resource.Id.actionDelete:
				DeletePOI ();
				return true;
			default:
				return base.OnOptionsItemSelected (item);
			}
		}

		protected void SavePOI()
		{
			bool errors = false;

			//Name 欄位不可空白
			if (String.IsNullOrEmpty (_nameEditText.Text)) {
				_nameEditText.Error = "Name欄位不可空白";
				errors = true;
			} else {
				_nameEditText.Error = null;
			}
			//檢查 Latitud 及 Longtitude 欄位的數值是否在合理範圍內
			double? tempLatitude = null;
			if (!String.IsNullOrEmpty (_latEditText.Text)) {
				try{
					tempLatitude = Double.Parse(_latEditText.Text);
					if ((tempLatitude > 90) | (tempLatitude < -90)) {
							_latEditText.Error = "Latitude的數值必須介於 -90 ~ 90之間";
							errors = true;
						}
						else{
							_latEditText.Error = null;
						}
				}catch{
					_latEditText.Error = "Latitude 必須是合法的浮點運算數值";
					errors = true;
				}
			}

			double? tempLongitude = null;
			if (!String.IsNullOrEmpty (_longEditText.Text)) {
				try{
					tempLongitude = Double.Parse(_longEditText.Text);
					if ((tempLongitude > 180) | (tempLongitude < -180)) {
						_longEditText.Error = "Longitude的數值必須介於-180~180之間";
						errors = true;
					}
					else{
						_longEditText.Error = null;
					}
				}catch{
					_longEditText.Error = "Longitude 必須是合法的浮點運算數值";
					errors = true;
				}
			}

			if (!errors) {
				_poi.Name = _nameEditText.Text;
				_poi.Description = _descrEditText.Text;
				_poi.Address = _addrEditText.Text;
				_poi.Latitude = tempLatitude;
				_poi.Longitude = tempLongitude;

				POIData.Service.SavePOI (_poi);
				Finish ();
			}
		}

		protected void DeletePOI()
		{
			AlertDialog.Builder alertConfirm = new AlertDialog.Builder (this);
			alertConfirm.SetCancelable (false);
			alertConfirm.SetPositiveButton ("確定", ConfirmDelete);
			alertConfirm.SetNegativeButton ("取消", delegate {
			});
			alertConfirm.SetMessage (String.Format("確定要刪除 {0}", _poi.Name));
			alertConfirm.Show ();
		}

		protected void ConfirmDelete(object sender, EventArgs e)
		{
			POIData.Service.DeletePOI (_poi);
			Toast toast = Toast.MakeText(this, String.Format("{0} 已刪除！", _poi.Name), ToastLength.Short);
			toast.Show();
			Finish ();
		}

		protected void GetLocationClicked(object sender, EventArgs e)
		{
			_obtainingLocation = true;
			_progressDialog = ProgressDialog.Show (this, "", "正在讀取地點資料中...");

			Criteria criteria = new Criteria ();
			criteria.Accuracy = Accuracy.NoRequirement;
			criteria.PowerRequirement = Power.NoRequirement;
			//Console.WriteLine ("Before RequestSingleUpdate...");
			_locMgr.RequestSingleUpdate (criteria, this, null);
			//Console.WriteLine (String.Format("After RequestSingleUpdate... {0}", criteria.ToString()));

		}

		public void MapClicked(object sender, EventArgs e)
		{
			Android.Net.Uri geoUri;
			if (String.IsNullOrEmpty (_addrEditText.Text)) {
				geoUri = Android.Net.Uri.Parse (String.Format ("geo:{0},{1}", _poi.Latitude, _poi.Longitude));
			} else {
				geoUri = Android.Net.Uri.Parse (String.Format ("geo:0,0?q={0}", _addrEditText.Text));
			}

			Intent mapIntent = new Intent (Intent.ActionView, geoUri);

			PackageManager packagemanager = PackageManager;
			IList<ResolveInfo> activities = packagemanager.QueryIntentActivities (mapIntent, 0);
			if (activities.Count == 0) {
				AlertDialog.Builder alertConfirm = new AlertDialog.Builder (this);
				alertConfirm.SetCancelable (false);
				alertConfirm.SetPositiveButton ("確定", delegate {
				});
				alertConfirm.SetMessage ("找不到地圖應用程式！");
				alertConfirm.Show ();
			} else {
				StartActivity (mapIntent);
			}

		}

		//Tom: 以地址擷取經緯度資料
		protected void GetLocationFromAddress(object sender, EventArgs e)
		{
			if (!String.IsNullOrEmpty (_addrEditText.Text)) {
				Geocoder geo = new Geocoder (this);
				IList<Address> addr = geo.GetFromLocationName (_addrEditText.Text, 1);
				try{
					//Tom: 如果定位系統有傳回資料，則判斷是否有經緯度資訊
					if (addr.First().HasLatitude && addr.First ().HasLongitude) {
						_latEditText.Text = addr.First ().Latitude.ToString();
						_longEditText.Text = addr.First ().Longitude.ToString();
					}
				}catch{
					//Tom: 定位系統未傳回資料，addr 是 null or empty
					AlertDialog.Builder alertConfirm = new AlertDialog.Builder (this);
					alertConfirm.SetCancelable (false);
					alertConfirm.SetPositiveButton ("定位", GetLocationClicked);
					alertConfirm.SetNegativeButton ("取消", delegate {});
					alertConfirm.SetTitle ("無經緯度資訊!");
					alertConfirm.SetMessage (String.Format("此地址查無經緯度資訊，請按[定位]以取得目前所在位置經緯度\n"+
						"，或按[取消]暫時忽略經緯度。"));
					alertConfirm.Show ();
				}
			}
		}

		public void OnLocationChanged (Location location)
		{
			_latEditText.Text = location.Latitude.ToString ();
			_longEditText.Text = location.Longitude.ToString ();
			//Console.WriteLine ("In OnLocationChanged...");
			Geocoder geocdr = new Geocoder (this);
			IList<Address> addresses = geocdr.GetFromLocation (location.Latitude, location.Longitude, 5);
			if (addresses.Any ()) {
				UpdateAddressFields (addresses.First ());
			}

			_progressDialog.Cancel ();
			_obtainingLocation = false;

			//Tom: 加入訊息 20141031
			Toast toast = Toast.MakeText (this, "定位完成。若地址不完全正確，請再手動修正。", ToastLength.Short);
			toast.Show ();
			_addrEditText.RequestFocus ();
		}

		protected void UpdateAddressFields(Address addr)
		{
			if (String.IsNullOrEmpty (_nameEditText.Text)) {
				_nameEditText.Text = addr.FeatureName;
			}

			if (String.IsNullOrEmpty (_addrEditText.Text)) {
				for (int i = 0; i < addr.MaxAddressLineIndex; i++) {
					if(!String.IsNullOrEmpty(_addrEditText.Text))
						_addrEditText.Text += System.Environment.NewLine;
					_addrEditText.Text += addr.GetAddressLine (i);
				}
			}
		}

		public void OnProviderDisabled (string provider)
		{
		}

		public void OnProviderEnabled (string provider)
		{
		}

		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
		}

		protected override void OnSaveInstanceState(Bundle outState)
		{
			base.OnSaveInstanceState (outState);
			outState.PutBoolean ("obtaininglocation", _obtainingLocation);
			//If we were waiting on location updates: cancel
			if (_obtainingLocation) {
				_locMgr.RemoveUpdates (this);
			}
		}

		protected override void OnRestoreInstanceState(Bundle savedInstanceState)
		{
			base.OnSaveInstanceState (savedInstanceState);
			_obtainingLocation = savedInstanceState.GetBoolean ("obtaininglocation");
			//If we were waiting on location updates: restart
			if (_obtainingLocation) {
				GetLocationClicked (this, new EventArgs ());
			}
		}

		public void NewPhotoClicked(object sender, EventArgs e)
		{
			if (!_poi.Id.HasValue) {
				AlertDialog.Builder alertConfirm = new AlertDialog.Builder (this);
				alertConfirm.SetCancelable (false);
				alertConfirm.SetPositiveButton ("確定", delegate {
				});
				alertConfirm.SetMessage ("請先將 POI 資料存檔後才能再附加照片！");
				alertConfirm.Show ();
			} else {
				Intent cameraIntent = new Intent (MediaStore.ActionImageCapture);

				PackageManager packagemaker = PackageManager;
				IList<ResolveInfo> activities = packagemaker.QueryIntentActivities (cameraIntent, 0);
				if (activities.Count == 0) {
					//display alert indication there is no camera apps
					AlertDialog.Builder alertConfirm = new AlertDialog.Builder(this);
					alertConfirm.SetCancelable(false);
					alertConfirm.SetPositiveButton("確定", delegate {});
					alertConfirm.SetMessage("無照相程式可用來擷取照片！");
					alertConfirm.Show ();
				} else {
					//launch the cameraIntent
					Java.IO.File imageFile = new Java.IO.File (POIData.Service.GetImageFilename (_poi.Id.Value));
					Android.Net.Uri imageUri = Android.Net.Uri.FromFile (imageFile);
					cameraIntent.PutExtra (MediaStore.ExtraOutput, imageUri);
					cameraIntent.PutExtra (MediaStore.ExtraSizeLimit, 1.5 * 1024);
					StartActivityForResult (cameraIntent, CAPTURE_PHOTO);
				}
			}
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			if (requestCode == CAPTURE_PHOTO) {
				if (resultCode == Result.Ok) {
					//display saved image
					Bitmap poiImage = POIData.GetImageFile (_poi.Id.Value);
					_poiImageView.SetImageBitmap (poiImage);
					if (poiImage != null) {
						_poiImageView.Visibility = ViewStates.Visible;
						poiImage.Dispose ();
					}
				} else {
					//Let the user know that the photo was cancelled
					Toast toast = Toast.MakeText (this, "未擷取任何影像！", ToastLength.Short);
					toast.Show ();
				}
			} else {
				base.OnActivityResult (requestCode, resultCode, data);
			}
		}

	}
}

