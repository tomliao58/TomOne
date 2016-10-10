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
using Android.Graphics;

namespace TomOne
{
	public class POIListViewAdapter: BaseAdapter<PointOfInterest>
	{
		private readonly Activity _context;
		public Location CurrentLocation{ get; set; }

		public POIListViewAdapter (Activity context)
		{
			_context = context;
		}

		public override int Count
		{
			get{ return POIData.Service.POIs.Count;}
		}

		public override long GetItemId(int position)
		{
			return POIData.Service.POIs [position].Id.Value;
		}

		public override PointOfInterest this[int position]
		{
			get{ return POIData.Service.POIs [position]; }
		}

		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			View view = convertView;
			if (view == null)
				view = _context.LayoutInflater.Inflate (Resource.Layout.POIListItem, null);

			PointOfInterest poi = POIData.Service.POIs [position];

			//load image into image view
			Bitmap poiImage = POIData.GetImageFile (poi.Id.Value);
			view.FindViewById<ImageView> (Resource.Id.poiImageView).SetImageBitmap (poiImage);
			if (poiImage != null) {
				poiImage.Dispose ();
			}
			view.FindViewById<TextView> (Resource.Id.nameTextView).Text = poi.Name;

			if (string.IsNullOrEmpty (poi.Address))
				view.FindViewById<TextView> (Resource.Id.addrTextView).Visibility = ViewStates.Gone;
			else
				view.FindViewById<TextView> (Resource.Id.addrTextView).Text = poi.Address;

			if ((CurrentLocation != null) && (poi.Latitude.HasValue) && (poi.Longitude.HasValue)) {
				Location poiLocation = new Location ("");
				poiLocation.Latitude = poi.Latitude.Value;
				poiLocation.Longitude = poi.Longitude.Value;
				float distance = CurrentLocation.DistanceTo (poiLocation) / 1000; //原單位為公尺；若乘以 0.000621371F表以英哩顯示;
				view.FindViewById<TextView> (Resource.Id.distanceTextView).Text = 
					String.Format ("{0:0,0.00} 公里", distance);
			} else {
				view.FindViewById<TextView> (Resource.Id.distanceTextView).Text = "??";
			}

			return view;
		}
	}
}

