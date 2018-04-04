﻿using System;
using System.ComponentModel.DataAnnotations;
using MountainWarehouse.EasyMWS.Enums;
using MountainWarehouse.EasyMWS.Model;
using Newtonsoft.Json;

namespace MountainWarehouse.EasyMWS.Data
{
	public class ReportRequestCallback
	{
		[Key]
		public int Id { get; set; }

		public int RequestRetryCount { get; set; }
		public DateTime LastRequested { get; set; }

		#region Serialized callback data necessary to invoke a method with it's argument values.

		public string TypeName { get; set; }
		public string MethodName { get; set; }
		public string Data { get; set; }
		public string DataTypeName { get; set; }

		#endregion

		#region Data necessary to request a report from amazon.

		public AmazonRegion AmazonRegion { get; set; }
		public string MerchantId { get; set; }
		public ContentUpdateFrequency ContentUpdateFrequency { get; set; }
		public string ReportRequestData { get; set; }

		#endregion

		#region Additional data generated by amazon in the process of fetching reports

		/// <summary>The ID that Amazon has given us for this requested report</summary>
		public string RequestReportId { get; set; }

		/// <summary>The ID that Amazon gives us when the report has been generated (required to download the report)</summary>
		public string GeneratedReportId { get; set; }

		#endregion



		public ReportRequestCallback()
		{
		}

		public ReportRequestCallback(Callback callback, string reportRequestData)
		{
			if(callback == null || string.IsNullOrEmpty(reportRequestData))
				throw new ArgumentException("Callback data or ReportRequestData not provided, but are required.");

			TypeName = callback.TypeName;
			MethodName = callback.MethodName;
			Data = callback.Data;
			DataTypeName = callback.DataTypeName;
			ReportRequestData = reportRequestData;
		}
	}

	internal static class ReportRequestCallbackExtensions
	{
		internal static ReportRequestPropertiesContainer GetPropertiesContainer(this ReportRequestCallback source)
		{
			return JsonConvert.DeserializeObject<ReportRequestPropertiesContainer>(source.ReportRequestData);
		}

		internal static string GetReportType(this ReportRequestCallback source)
		{
			return GetPropertiesContainer(source)?.ReportType;
		}

		internal static string GetRegionAndTypeString(this ReportRequestCallback source)
		{
			return $"[region:'{source.AmazonRegion.ToString()}', reportType:'{GetReportType(source)}']" ;
		}
	}
}
