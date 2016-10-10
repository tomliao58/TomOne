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

namespace TomOne
{
	[Activity (Label = "景點管理", MainLauncher = true, Icon = "@drawable/t_studio_w", ConfigurationChanges=
		(Android.Content.PM.ConfigChanges.Orientation|Android.Content.PM.ConfigChanges.ScreenSize))]
	public class POIListActivity : Activity, ILocationListener
	{
		ListView _poiListView;
		POIListViewAdapter _adapter;
		LocationManager _locMgr;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "POIList" layout resource
			SetContentView (Resource.Layout.POIList);

			_locMgr = GetSystemService (Context.LocationService) as LocationManager;

			_poiListView = FindViewById<ListView> (Resource.Id.poiListView);
			_adapter = new POIListViewAdapter (this);
			_poiListView.Adapter = _adapter;
			_poiListView.ItemClick += POIClicked;

			//Tom: If POIList is empty, then call POIDetailActivity
			if (_poiListView.Count == 0) {
				AlertDialog.Builder alertConfirm = new AlertDialog.Builder (this);
				alertConfirm.SetCancelable (false);
				alertConfirm.SetPositiveButton ("確定", AddNewPOI);
				alertConfirm.SetNegativeButton ("取消", delegate {});
				alertConfirm.SetTitle ("景點管理");
				alertConfirm.SetMessage (String.Format("請按[確定]鍵新增資料，\n或按[取消]回主畫面。\n\n" +
					"ps:取消之後若欲再新增景點資料，請按螢幕右上角之\"＋\"號。"));
				alertConfirm.Show ();
			}
		}

		protected override void OnResume()
		{
			base.OnResume ();
			_adapter.NotifyDataSetChanged ();

			Criteria criteria = new Criteria ();
			criteria.Accuracy = Accuracy.Fine;
			criteria.PowerRequirement = Power.High;

			string provider = _locMgr.GetBestProvider (criteria, true);
			_locMgr.RequestLocationUpdates (provider, 20000, 100, this);
		}

		protected override void OnPause()
		{
			base.OnPause ();
			_locMgr.RemoveUpdates (this);
		}

		//Tom:呼叫 POIDetailActivity
		protected void AddNewPOI(object sender, EventArgs e)
		{
			StartActivity (typeof(POIDetailActivity));
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			MenuInflater.Inflate (Resource.Menu.POIListViewMenu, menu);
			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId) 
			{
			case Resource.Id.actionNew:
				//place holder for creating new poi
				//StartActivity (typeof(POIDetailActivity));
				AddNewPOI(this,null);
				return true;
			case Resource.Id.actionRefresh:
				POIData.Service.RefreshCache ();
				_adapter.NotifyDataSetChanged ();
				//_poiListView.FindViewById<TextView> (Resource.Id.fileTextView).Text = Android.OS.Environment.ExternalStorageDirectory.Path;
				return true;
			default:
				return base.OnOptionsItemSelected (item);
			}
		}

		protected void POIClicked(object sender, ListView.ItemClickEventArgs e)
		{
			//PointOfInterest poi = POIData.Service.GetPOI ((int)e.Id);
			//Console.WriteLine ("POIClicked Name is {0}", poi.Name);
			Intent poiDetailIntent = new Intent (this, typeof(POIDetailActivity));
			poiDetailIntent.PutExtra ("poiId", (int)e.Id);
			StartActivity (poiDetailIntent);
		}

		public void OnLocationChanged(Location location)
		{
			_adapter.CurrentLocation = location;
			_adapter.NotifyDataSetChanged ();
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

	}
}


