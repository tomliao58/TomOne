using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TomOne
{
	public class POIJsonService : IPOIDataService
	{
		private string _storagePath;
		private List<PointOfInterest> _pois = new List<PointOfInterest>();

		public POIJsonService (string storagePath)
		{
			_storagePath = storagePath;
			//create storage path if it does not exit
			if(!Directory.Exists(_storagePath))
				Directory.CreateDirectory(_storagePath);
			RefreshCache ();
		}

		#region IPOIDataService implementation
		public void RefreshCache ()
		{
			_pois.Clear ();

			string[] filenames = Directory.GetFiles (_storagePath, "*.json");

			foreach (string filename in filenames) {
				string poiString = File.ReadAllText (filename);
				PointOfInterest poi = JsonConvert.DeserializeObject<PointOfInterest> (poiString);
				_pois.Add (poi);
			}
		}
		public PointOfInterest GetPOI (int id)
		{
			PointOfInterest poi = _pois.Find (p => p.Id == id);
			return poi;
		}

		private int GetNextId()
		{
			if (_pois.Count == 0)
				return 1;
			else
				return _pois.Max (p => p.Id.Value) + 1;
		}

		private string GetFilename(int id)
		{
			return Path.Combine (_storagePath, "poi" + id.ToString () + ".json");
		}

		public void SavePOI (PointOfInterest poi)
		{
			Boolean newPOI = false;
			if (!poi.Id.HasValue) {
				poi.Id = GetNextId ();
				newPOI = true;
			}
			//serialize POI
			string poiString = JsonConvert.SerializeObject (poi);
			//write new file or overwrite the existing file
			File.WriteAllText (GetFilename (poi.Id.Value), poiString);
			//update cache if file save was successful
			if (newPOI)
				_pois.Add (poi);
		}

		public void DeletePOI (PointOfInterest poi)
		{
			//delete POI JSON file
			if(File.Exists(GetFilename(poi.Id.Value)))
				File.Delete (GetFilename (poi.Id.Value));

			//delete POI image file
			if(File.Exists(GetImageFilename(poi.Id.Value)))
					File.Delete(GetImageFilename(poi.Id.Value));
			//remove POI from cache
			_pois.Remove (poi);
		}

		public IReadOnlyList<PointOfInterest> POIs {
			get { return _pois;}
		}

		public string GetImageFilename (int id)
		{
			return Path.Combine (_storagePath, "poiimage" + id.ToString () + ".jpg");
		}
		#endregion
	}
}

