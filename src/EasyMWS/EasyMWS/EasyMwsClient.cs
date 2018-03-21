﻿using System;
using System.IO;
using System.Linq;
using MountainWarehouse.EasyMWS.Data;
using MountainWarehouse.EasyMWS.Helpers;
using MountainWarehouse.EasyMWS.Logging;
using MountainWarehouse.EasyMWS.Processors;
using MountainWarehouse.EasyMWS.Services;
using MountainWarehouse.EasyMWS.WebService.MarketplaceWebService;
using Newtonsoft.Json;

namespace MountainWarehouse.EasyMWS
{
	/// <summary>Client for Amazon Marketplace Web Services</summary>
	public class EasyMwsClient
	{
		private IMarketplaceWebServiceClient _mwsClient;
		private IReportRequestCallbackService _reportRequestCallbackService;
		private IFeedSubmissionCallbackService _feedSubmissionCallbackService;
		private CallbackActivator _callbackActivator;
		private string _merchantId;
		private AmazonRegion _amazonRegion;
		private IRequestReportProcessor _requestReportProcessor;
		private IFeedSubmissionProcessor _feedSubmissionProcessor;
		private EasyMwsOptions _options;
		private IEasyMwsLogger _logger;


		public AmazonRegion AmazonRegion => _amazonRegion;

		internal EasyMwsClient(AmazonRegion region, string merchantId, string accessKeyId, string mwsSecretAccessKey, IFeedSubmissionCallbackService feedSubmissionCallbackService, IReportRequestCallbackService reportRequestCallbackService, IMarketplaceWebServiceClient marketplaceWebServiceClient, IRequestReportProcessor requestReportProcessor, IFeedSubmissionProcessor feedSubmissionProcessor, IEasyMwsLogger easyMwsLogger, EasyMwsOptions options = null) 
			: this(region, merchantId, accessKeyId, mwsSecretAccessKey, easyMwsLogger, options)
		{
			_reportRequestCallbackService = reportRequestCallbackService;
			_feedSubmissionCallbackService = feedSubmissionCallbackService;
			_requestReportProcessor = requestReportProcessor;
			_feedSubmissionProcessor = feedSubmissionProcessor;
			_mwsClient = marketplaceWebServiceClient;
		}

		/// <param name="region">The region of the account</param>
		/// <param name="merchantId">Seller ID. Required parameter.</param>
		/// <param name="accessKeyId">Your specific access key. Required parameter.</param>
		/// <param name="mwsSecretAccessKey">Your specific secret access key. Required parameter.</param>
		/// <param name="easyMwsLogger">An optional IEasyMwsLogger instance that can provide access to logs. It is strongly recommended to use a logger implementation already existing in the EasyMws package.</param>
		/// <param name="options">Configuration options for EasyMwsClient</param>
		public EasyMwsClient(AmazonRegion region, string merchantId, string accessKeyId, string mwsSecretAccessKey, IEasyMwsLogger easyMwsLogger = null, EasyMwsOptions options = null)
		{
			if(string.IsNullOrEmpty(merchantId) || accessKeyId == null || mwsSecretAccessKey == null)
				throw new ArgumentNullException("One or more required parameters provided to initialize the EasyMwsClient were null or empty.");

			_options = options ?? EasyMwsOptions.Defaults;
			_merchantId = merchantId;
			_amazonRegion = region;
			_mwsClient = new MarketplaceWebServiceClient(accessKeyId, mwsSecretAccessKey, CreateConfig(region));
			_reportRequestCallbackService = _reportRequestCallbackService ?? new ReportRequestCallbackService();
			_feedSubmissionCallbackService = _feedSubmissionCallbackService ?? new FeedSubmissionCallbackService();
			_callbackActivator = new CallbackActivator();
			_requestReportProcessor = new RequestReportProcessor(_mwsClient, _reportRequestCallbackService, _options);
			_feedSubmissionProcessor = new FeedSubmissionProcessor(_mwsClient, _feedSubmissionCallbackService, _options);
			_logger = easyMwsLogger ?? new EasyMwsLogger(isEnabled: false);
		}

		/// <summary>
		/// Method that handles querying amazon for reports that are queued for download with the EasyMwsClient.QueueReport method.
		/// It is handling the following operations : 
		/// 1. Requests the next report from report request queue from Amazon, if a ReportRequestId is successfully generated by amazon then the ReportRequest is moved in a queue of reports awaiting Amazon generation.
		///		If a ReportRequestId is not generated by amazon, a retry policy will be applied (retrying to get a ReportRequestId from amazon at after 30m, 1h, 4h intervals.)
		/// 2. Query amazon if any of the reports that are pending generation, were generated.
		///		If any reports were successfully generated (returned report processing status is "_DONE_"), those reports are moved to a queue of reports that await downloading.
		///		If any reports requests were canceled by amazon (returned report processing status is "_CANCELLED_"), then those ReportRequests are moved back to the report request queue.
		///		If amazon returns a processing status any other than "_DONE_" or "_CANCELLED_" for any report requests, those ReportRequests are moved back to the report request queue.
		/// 3. Downloads the next report from amazon (which is the next report ReportRequest in the queue of reports awaiting download).
		/// 4. Perform a callback of the handler method provided as argument when QueueReport was called. The report content can be obtained by reading the stream argument of the callback method.
		/// </summary>
		public void Poll()
		{
			_logger.Info("Polling operation has been triggered!");
			try
			{
				//TODO: For each request of any kind made to amazon, record RequestId and Timestamp. Either retain these for 30 days (config option On/Off) and/or return this info to the caller.
				//For more info see: https://docs.developer.amazonservices.com/en_US/dev_guide/DG_ResponseFormat.html

				//TODO: Whenever an amazon request is unsuccessful, log the error, the RequestId and Timestamp found on ErrorResponse. (and take additional appropriate action if it's the case)
				// In order to access this information, I believe MarketplaceWebServiceException has to be caught. Request info can be found on the ResponseHeaderMetadata property of the ex.
				PollReports();
				PollFeeds();
			}
			catch (Exception e)
			{
				_logger.Error(e.Message, e);
			}
		}

		/// <summary>
		/// Add a new ReportRequest to a queue of requests that are going to be processed, with the final result of trying to download the respective report from Amazon.
		/// </summary>
		/// <param name="reportRequestContainer">An object that contains the arguments required to request the report from Amazon. This object is meant to be obtained by calling a ReportRequestFactory, ex: IReportRequestFactoryFba.</param>
		/// <param name="callbackMethod">A delegate for a method that is going to be called once a report has been downloaded from amazon. The 'Stream' argument of that method will contain the actual report content.</param>
		/// <param name="callbackData">An object any argument(s) needed to invoke the delegate 'callbackMethod'</param>
		public void QueueReport(ReportRequestPropertiesContainer reportRequestContainer, Action<Stream, object> callbackMethod, object callbackData)
		{
			_reportRequestCallbackService.Create(GetSerializedReportRequestCallback(reportRequestContainer, callbackMethod, callbackData));
			_reportRequestCallbackService.SaveChanges();
		}

		/// <summary>
		/// Add a new FeedSubmissionRequest to a queue of feeds to be submitted to amazon, with the final result of obtaining of posting the feed data to amazon and obtaining a response.
		/// </summary>
		/// <param name="feedSubmissionContainer"></param>
		/// <param name="callbackMethod"></param>
		/// <param name="callbackData"></param>
		public void QueueFeed(FeedSubmissionPropertiesContainer feedSubmissionContainer, Action<Stream, object> callbackMethod, object callbackData)
		{
			_feedSubmissionCallbackService.Create(GetSerializedFeedSubmissionCallback(feedSubmissionContainer, callbackMethod, callbackData));
			_feedSubmissionCallbackService.SaveChanges();
		}

		private void PollReports()
		{
			CleanUpReportRequestQueue();
			RequestNextReportInQueueFromAmazon();
			RequestReportStatusesFromAmazon();
			var generatedReportRequestCallback = DownloadNextGeneratedRequestReportInQueueFromAmazon();
			PerformCallback(generatedReportRequestCallback.reportRequestCallback, generatedReportRequestCallback.stream);
			_reportRequestCallbackService.SaveChanges();
		}

		private void PollFeeds()
		{
			CleanUpFeedSubmissionQueue();
			SubmitNextFeedInQueueToAmazon();
			RequestFeedSubmissionStatusesFromAmazon();
			var amazonProcessingReport = RequestNextFeedSubmissionInQueueFromAmazon();
			PerformCallback(amazonProcessingReport.feedSubmissionCallback, amazonProcessingReport.reportContent);
			_feedSubmissionCallbackService.SaveChanges();
		}

		private void CleanUpFeedSubmissionQueue()
		{
			var expiredFeedSubmission = _feedSubmissionCallbackService.GetAll()
				.Where(rrc => rrc.SubmissionRetryCount > _options.FeedSubmissionMaxRetryCount);

			foreach (var feedSubmission in expiredFeedSubmission)
			{
				_feedSubmissionCallbackService.Delete(feedSubmission);
			}
		}

		private void SubmitNextFeedInQueueToAmazon()
		{
			var feedSubmission = _feedSubmissionProcessor.GetNextFeedToSubmitFromQueue(_amazonRegion, _merchantId);

			if(feedSubmission == null)
				return;

			var feedSubmissionId = _feedSubmissionProcessor.SubmitSingleQueuedFeedToAmazon(feedSubmission, _merchantId);

			feedSubmission.LastSubmitted = DateTime.UtcNow;
			_feedSubmissionCallbackService.Update(feedSubmission);

			if (string.IsNullOrEmpty(feedSubmissionId))
			{
				_feedSubmissionProcessor.AllocateFeedSubmissionForRetry(feedSubmission);
			}
			else
			{
				_feedSubmissionProcessor.MoveToQueueOfSubmittedFeeds(feedSubmission, feedSubmissionId);	
			}
		}

		private void RequestNextReportInQueueFromAmazon()
		{
			var reportRequestCallbackReportQueued = _requestReportProcessor.GetNonRequestedReportFromQueue(_amazonRegion, _merchantId);

			if (reportRequestCallbackReportQueued == null)
				return;

			var reportRequestId = _requestReportProcessor.RequestSingleQueuedReport(reportRequestCallbackReportQueued, _merchantId);

			reportRequestCallbackReportQueued.LastRequested = DateTime.UtcNow;
			_reportRequestCallbackService.Update(reportRequestCallbackReportQueued);
			
			if (string.IsNullOrEmpty(reportRequestId))
			{
				_requestReportProcessor.AllocateReportRequestForRetry(reportRequestCallbackReportQueued);
			}
			else
			{
				_requestReportProcessor.MoveToNonGeneratedReportsQueue(reportRequestCallbackReportQueued, reportRequestId);
			}
		}

		private void CleanUpReportRequestQueue()
		{
			var expiredReportRequests = _reportRequestCallbackService.GetAll()
				.Where(rrc => rrc.RequestRetryCount > _options.ReportRequestMaxRetryCount);

			foreach (var reportRequest in expiredReportRequests)
			{
				_reportRequestCallbackService.Delete(reportRequest);
			}
		}

		private (FeedSubmissionCallback feedSubmissionCallback, Stream reportContent) RequestNextFeedSubmissionInQueueFromAmazon()
		{
			var nextFeedWithProcessingComplete = _feedSubmissionProcessor.GetNextFeedFromProcessingCompleteQueue(_amazonRegion, _merchantId);

			if (nextFeedWithProcessingComplete == null) return (null, null);

			var processingReportInfo = _feedSubmissionProcessor.QueryFeedProcessingReport(nextFeedWithProcessingComplete, _merchantId);

			// TODO: If feed processing report Content-MD5 hash doesn't match the hash sent by amazon, retry up to 3 times.
			// log a warning for each hash miss-match, and recommend to the user to notify Amazon that a corrupted body was received.

			return (nextFeedWithProcessingComplete, processingReportInfo.processingReport);
		}

		private (ReportRequestCallback reportRequestCallback, Stream stream) DownloadNextGeneratedRequestReportInQueueFromAmazon()
		{
			var generatedReportRequest = _requestReportProcessor.GetReadyForDownloadReports(_amazonRegion, _merchantId);

			if (generatedReportRequest == null)
				return (null, null);
			
			var stream = _requestReportProcessor.DownloadGeneratedReport(generatedReportRequest, _merchantId);
			
			return (generatedReportRequest, stream);
		}

		private void PerformCallback(ReportRequestCallback reportRequestCallback, Stream stream)
		{
			if (reportRequestCallback == null || stream == null) return;

			var callback = new Callback(reportRequestCallback.TypeName, reportRequestCallback.MethodName,
				reportRequestCallback.Data, reportRequestCallback.DataTypeName);

			_callbackActivator.CallMethod(callback, stream);

			DequeueReport(reportRequestCallback);
		}

		private void PerformCallback(FeedSubmissionCallback feedSubmissionCallback, Stream stream)
		{
			if (feedSubmissionCallback == null || stream == null) return;

			var callback = new Callback(feedSubmissionCallback.TypeName, feedSubmissionCallback.MethodName,
				feedSubmissionCallback.Data, feedSubmissionCallback.DataTypeName);

			_callbackActivator.CallMethod(callback, stream);

			_feedSubmissionProcessor.DequeueFeedSubmissionCallback(feedSubmissionCallback);
		}

		private void RequestFeedSubmissionStatusesFromAmazon()
		{
			var submittedFeeds = _feedSubmissionProcessor.GetAllSubmittedFeeds(_amazonRegion, _merchantId).ToList();

			if (!submittedFeeds.Any())
				return;

			var feedSubmissionIdList = submittedFeeds.Select(x => x.FeedSubmissionId);

			var feedSubmissionResults = _feedSubmissionProcessor.GetFeedSubmissionResults(feedSubmissionIdList, _merchantId);

			_feedSubmissionProcessor.MoveFeedsToQueuesAccordingToProcessingStatus(feedSubmissionResults);
		}

		private void RequestReportStatusesFromAmazon()
		{
			var reportRequestCallbacksPendingReports = _requestReportProcessor.GetAllPendingReport(_amazonRegion, _merchantId).ToList();

			if (!reportRequestCallbacksPendingReports.Any())
				return;

			var reportRequestIds = reportRequestCallbacksPendingReports.Select(x => x.RequestReportId);

			var reportRequestStatuses = _requestReportProcessor.GetReportRequestListResponse(reportRequestIds, _merchantId);

			_requestReportProcessor.MoveReportsToGeneratedQueue(reportRequestStatuses);
			_requestReportProcessor.MoveReportsBackToRequestQueue(reportRequestStatuses);

		}

		private void DequeueReport(ReportRequestCallback reportRequestCallback)
		{
			_requestReportProcessor.DequeueReportRequestCallback(reportRequestCallback);
		}

		private ReportRequestCallback GetSerializedReportRequestCallback(
			ReportRequestPropertiesContainer reportRequestContainer, Action<Stream, object> callbackMethod, object callbackData)
		{
			if (reportRequestContainer == null || callbackMethod == null) throw new ArgumentNullException();
			var serializedCallback = _callbackActivator.SerializeCallback(callbackMethod, callbackData);

			return new ReportRequestCallback(serializedCallback)
			{
				AmazonRegion = _amazonRegion,
				MerchantId = _merchantId,
				LastRequested = DateTime.MinValue,
				ContentUpdateFrequency = reportRequestContainer.UpdateFrequency,
				RequestReportId = null,
				GeneratedReportId = null,
				RequestRetryCount = 0,
				ReportRequestData = JsonConvert.SerializeObject(reportRequestContainer)
			};
		}

		private FeedSubmissionCallback GetSerializedFeedSubmissionCallback(
			FeedSubmissionPropertiesContainer propertiesContainer, Action<Stream, object> callbackMethod, object callbackData)
		{
			if (propertiesContainer == null || callbackMethod == null) throw new ArgumentNullException();
			var serializedCallback = _callbackActivator.SerializeCallback(callbackMethod, callbackData);

			return new FeedSubmissionCallback(serializedCallback)
			{
				AmazonRegion = _amazonRegion,
				MerchantId = _merchantId,
				LastSubmitted = DateTime.MinValue,
				IsProcessingComplete = false,
				HasErrors = false,
				SubmissionErrorData = null,
				SubmissionRetryCount = 0,
				FeedSubmissionId = null,
				FeedSubmissionData = JsonConvert.SerializeObject(propertiesContainer),
			};
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
