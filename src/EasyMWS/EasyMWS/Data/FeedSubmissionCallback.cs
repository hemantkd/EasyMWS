﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MountainWarehouse.EasyMWS.Enums;
using MountainWarehouse.EasyMWS.Model;
using Newtonsoft.Json;

namespace MountainWarehouse.EasyMWS.Data
{
    public class FeedSubmissionCallback
    {
	    private string _regionAndType;

	    [NotMapped]
	    public string RegionAndTypeComputed
	    {
		    // this field is populated based on ReportRequestData which, once set in the ctor, should never change again for the same entity.
			get { return _regionAndType = _regionAndType ?? this.GetRegionAndTypeString(); }
	    }

	    [Key]
	    public int Id { get; set; }
	    public int SubmissionRetryCount { get; set; }
	    public DateTime LastSubmitted { get; set; }

		#region Serialized callback data necessary to invoke a method with it's argument values.
		public string TypeName { get; set; }
	    public string MethodName { get; set; }
	    public string Data { get; set; }
	    public string DataTypeName { get; set; }
		#endregion

		#region Data necessary to request a report from amazon.
	    public AmazonRegion AmazonRegion { get; set; }
	    public string MerchantId { get; set; }
		public string FeedSubmissionData { get; set; }
		#endregion

		#region Additional data generated by amazon in the process of fetching reports

	    public string FeedSubmissionId { get; set; }
	    public bool IsProcessingComplete { get; set; }
	    public bool HasErrors { get; set; }
	    public string SubmissionErrorData { get; set; }

	    #endregion

		public FeedSubmissionCallback()
		{
		}

		public FeedSubmissionCallback(Callback callback) => (TypeName, MethodName, Data, DataTypeName) =
			(callback.TypeName, callback.MethodName, callback.Data, callback.DataTypeName);
	}

	internal static class FeedSubmissionCallbackExtensions
	{
		internal static FeedSubmissionPropertiesContainer GetPropertiesContainer(this FeedSubmissionCallback source)
		{
			return JsonConvert.DeserializeObject<FeedSubmissionPropertiesContainer>(source.FeedSubmissionData);
		}

		internal static string GetRegionAndTypeString(this FeedSubmissionCallback source)
		{
			var feedType = GetPropertiesContainer(source)?.FeedType;
			return $"[region:'{source.AmazonRegion.ToString()}', feedType:'{feedType}']";
		}
	}
}
