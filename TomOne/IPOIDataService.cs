using System;
using System.Collections.Generic;

namespace TomOne
{
	public interface IPOIDataService
	{
		IReadOnlyList<PointOfInterest> POIs { get;}
		void RefreshCache();
		PointOfInterest GetPOI(int id);
		void SavePOI(PointOfInterest poi);
		void DeletePOI(PointOfInterest poi);
		string GetImageFilename (int id);
	}
}

