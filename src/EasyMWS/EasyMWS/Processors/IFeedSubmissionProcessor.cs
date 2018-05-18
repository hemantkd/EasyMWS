﻿using System.Collections.Generic;
using System.IO;
using MountainWarehouse.EasyMWS.Data;
using MountainWarehouse.EasyMWS.Enums;

namespace MountainWarehouse.EasyMWS.Processors
{
	internal interface IFeedSubmissionProcessor
	{
		FeedSubmissionEntry GetNextFromQueueOfFeedsToSubmit();
		string SubmitFeedToAmazon(FeedSubmissionEntry feedSubmission);
		void MoveToQueueOfSubmittedFeeds(FeedSubmissionEntry feedSubmission, string feedSubmissionId);
		IEnumerable<string> GetIdsForSubmittedFeedsFromQueue();

		List<(string FeedSubmissionId, string FeedProcessingStatus)> RequestFeedSubmissionStatusesFromAmazon(
			IEnumerable<string> feedSubmissionIdList, string merchant);

		void QueueFeedsAccordingToProcessingStatus(
			List<(string FeedSubmissionId, string FeedProcessingStatus)> feedProcessingStatuses);

		FeedSubmissionEntry GetNextFromQueueOfProcessingCompleteFeeds();
		(Stream processingReport, string md5hash) GetFeedSubmissionResultFromAmazon(FeedSubmissionEntry feedSubmissionEntry);
		void RemoveFromQueue(int feedSubmissionId);
		void MoveToRetryQueue(FeedSubmissionEntry feedSubmission);
		void CleanUpFeedSubmissionQueue();
	}
}
