﻿using System;
using System.IO;
using MountainWarehouse.EasyMWS.Enums;
using MountainWarehouse.EasyMWS.Helpers;
using MountainWarehouse.EasyMWS.Logging;
using MountainWarehouse.EasyMWS.Model;
using MountainWarehouse.EasyMWS.Processors;
using MountainWarehouse.EasyMWS.WebService.MarketplaceWebService;

namespace MountainWarehouse.EasyMWS
{
	/// <summary>Client for Amazon Marketplace Web Services</summary>
	public class EasyMwsClient
	{
		private readonly EasyMwsOptions _options;
		private readonly AmazonRegion _amazonRegion;
		private readonly string _merchantId;
		private readonly IQueueingProcessor<ReportRequestPropertiesContainer> _reportProcessor;
		private readonly IQueueingProcessor<FeedSubmissionPropertiesContainer> _feedProcessor;
		private IEasyMwsLogger _logger;

		internal EasyMwsClient(AmazonRegion region, string merchantId, string accessKeyId, string mwsSecretAccessKey,
			IQueueingProcessor<ReportRequestPropertiesContainer> reportProcessor,
			IQueueingProcessor<FeedSubmissionPropertiesContainer> feedProcessor, IEasyMwsLogger easyMwsLogger,
			EasyMwsOptions options)
			: this(region, merchantId, accessKeyId, mwsSecretAccessKey, easyMwsLogger, options)
		{
			_reportProcessor = reportProcessor;
			_feedProcessor = feedProcessor;
		}

		/// <param name="region">The region of the account</param>
		/// <param name="merchantId">Seller ID. Required parameter.</param>
		/// <param name="accessKeyId">Your specific access key. Required parameter.</param>
		/// <param name="mwsSecretAccessKey">Your specific secret access key. Required parameter.</param>
		/// <param name="easyMwsLogger">An optional IEasyMwsLogger instance that can provide access to logs. It is strongly recommended to use a logger implementation already existing in the EasyMws package.</param>
		/// <param name="options">Configuration options for EasyMwsClient</param>
		public EasyMwsClient(AmazonRegion region, string merchantId, string accessKeyId, string mwsSecretAccessKey,
			IEasyMwsLogger easyMwsLogger = null, EasyMwsOptions options = null)
		{
			if (string.IsNullOrEmpty(merchantId) || string.IsNullOrEmpty(accessKeyId) ||
			    string.IsNullOrEmpty(mwsSecretAccessKey))
				throw new ArgumentNullException(
					"One or more required parameters provided to initialize the EasyMwsClient were null or empty.");

			_amazonRegion = region;
			_merchantId = merchantId;
			_options = options ?? EasyMwsOptions.Defaults();

			_logger = easyMwsLogger ?? new EasyMwsLogger(isEnabled: false);
			var mwsClient = new MarketplaceWebServiceClient(accessKeyId, mwsSecretAccessKey, CreateConfig(_amazonRegion));
			_reportProcessor = _reportProcessor ?? new ReportProcessor(_amazonRegion, _merchantId, _options, mwsClient, _logger);
			_feedProcessor = _feedProcessor ?? new FeedProcessor(_amazonRegion, _merchantId, _options, mwsClient, _logger);

		}

		public AmazonRegion AmazonRegion => _amazonRegion;
		public string MerchantId => _merchantId;
		public EasyMwsOptions Options => _options;

		/// <summary>
		/// Method that handles querying amazon for reports that are queued for download with the EasyMwsClient.QueueReport method.
		/// It is handling the following operations : 
		/// 1. Requests the next report from report request queue from Amazon, if a ReportRequestId is successfully generated by amazon then the ReportRequest is moved in a queue of reports awaiting Amazon generation.
		///    If a ReportRequestId is not generated by amazon, a retry policy will be applied (retrying to get a ReportRequestId from amazon at after 30m, 1h, 4h intervals.)
		/// 2. Query amazon if any of the reports that are pending generation, were generated.
		///    If any reports were successfully generated (returned report processing status is "_DONE_"), those reports are moved to a queue of reports that await downloading.
		///    If any reports requests were canceled by amazon (returned report processing status is "_CANCELLED_"), then those ReportRequests are moved back to the report request queue.
		///    If amazon returns a processing status any other than "_DONE_" or "_CANCELLED_" for any report requests, those ReportRequests are moved back to the report request queue.
		/// 3. Downloads the next report from amazon (which is the next report ReportRequest in the queue of reports awaiting download).
		/// 4. Perform a callback of the handler method provided as argument when QueueReport was called. The report content can be obtained by reading the stream argument of the callback method.
		/// </summary>
		public void Poll()
		{
			_reportProcessor.Poll();
			_feedProcessor.Poll();
		}

		/// <summary>
		/// Add a new ReportRequest to a queue of requests that are going to be processed, with the final result of trying to download the respective report from Amazon.
		/// </summary>
		/// <param name="reportRequestContainer">An object that contains the arguments required to request the report from Amazon. This object is meant to be obtained by calling a ReportRequestFactory, ex: IReportRequestFactoryFba.</param>
		/// <param name="callbackMethod">A delegate for a method that is going to be called once a report has been downloaded from amazon. The 'Stream' argument of that method will contain the actual report content.</param>
		/// <param name="callbackData">An object any argument(s) needed to invoke the delegate 'callbackMethod'</param>
		public void QueueReport(ReportRequestPropertiesContainer reportRequestContainer,
			Action<Stream, object> callbackMethod, object callbackData)
		{
			_reportProcessor.Queue(reportRequestContainer, callbackMethod, callbackData);
		}

		/// <summary>
		/// Add a new FeedSubmissionRequest to a queue of feeds to be submitted to amazon, with the final result of obtaining of posting the feed data to amazon and obtaining a response.
		/// </summary>
		/// <param name="feedSubmissionContainer"></param>
		/// <param name="callbackMethod"></param>
		/// <param name="callbackData"></param>
		public void QueueFeed(FeedSubmissionPropertiesContainer feedSubmissionContainer,
			Action<Stream, object> callbackMethod, object callbackData)
		{
			_feedProcessor.Queue(feedSubmissionContainer, callbackMethod, callbackData);
		}

		#region Helpers for creating the MarketplaceWebServiceClient

		private MarketplaceWebServiceConfig CreateConfig(AmazonRegion region)
		{
			string rootUrl;
			switch (region)
			{
				case AmazonRegion.Australia:
					rootUrl = MwsEndpoint.Australia.RegionOrMarketPlaceEndpoint;
					break;
				case AmazonRegion.China:
					rootUrl = MwsEndpoint.China.RegionOrMarketPlaceEndpoint;
					break;
				case AmazonRegion.Europe:
					rootUrl = MwsEndpoint.Europe.RegionOrMarketPlaceEndpoint;
					break;
				case AmazonRegion.India:
					rootUrl = MwsEndpoint.India.RegionOrMarketPlaceEndpoint;
					break;
				case AmazonRegion.Japan:
					rootUrl = MwsEndpoint.Japan.RegionOrMarketPlaceEndpoint;
					break;
				case AmazonRegion.NorthAmerica:
					rootUrl = MwsEndpoint.NorthAmerica.RegionOrMarketPlaceEndpoint;
					break;
				case AmazonRegion.Brazil:
					rootUrl = MwsEndpoint.Brazil.RegionOrMarketPlaceEndpoint;
					break;
				default:
					throw new ArgumentException($"{region} is unknown - EasyMWS doesn't know the RootURL");
			}


			var config = new MarketplaceWebServiceConfig
			{
				ServiceURL = rootUrl
			};
			config = config.WithUserAgent("EasyMWS");

			return config;
		}

		#endregion

	}
}
