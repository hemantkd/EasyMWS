﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MountainWarehouse.EasyMWS.Data;

namespace MountainWarehouse.EasyMWS.Migrations
{
    [DbContext(typeof(EasyMwsContext))]
    partial class EasyMwsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.0-rtm-30799")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.FeedSubmissionDetails", b =>
                {
                    b.Property<int>("FeedSubmissionEntryId");

                    b.Property<byte[]>("FeedContent");

                    b.Property<byte[]>("FeedSubmissionReport");

                    b.HasKey("FeedSubmissionEntryId");

                    b.ToTable("FeedSubmissionDetails");
                });

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.FeedSubmissionEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AmazonRegion");

                    b.Property<string>("Data");

                    b.Property<string>("DataTypeName");

                    b.Property<DateTime>("DateCreated");

                    b.Property<int>("FeedProcessingRetryCount");

                    b.Property<string>("FeedSubmissionData");

                    b.Property<string>("FeedSubmissionId");

                    b.Property<int>("FeedSubmissionRetryCount");

                    b.Property<string>("FeedType");

                    b.Property<bool>("HasErrors");

                    b.Property<int>("InvokeCallbackRetryCount");

                    b.Property<bool>("IsLocked");

                    b.Property<bool>("IsProcessingComplete");

                    b.Property<string>("LastAmazonFeedProcessingStatus");

                    b.Property<DateTime>("LastSubmitted");

                    b.Property<string>("MerchantId");

                    b.Property<string>("MethodName");

                    b.Property<int>("ReportDownloadRetryCount");

                    b.Property<string>("SubmissionErrorData");

                    b.Property<string>("TypeName");

                    b.HasKey("Id");

                    b.HasIndex("FeedSubmissionId");

                    b.ToTable("FeedSubmissionEntries");
                });

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.ReportRequestDetails", b =>
                {
                    b.Property<int>("ReportRequestEntryId");

                    b.Property<byte[]>("ReportContent");

                    b.HasKey("ReportRequestEntryId");

                    b.ToTable("ReportRequestDetails");
                });

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.ReportRequestEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AmazonRegion");

                    b.Property<int>("ContentUpdateFrequency");

                    b.Property<string>("Data");

                    b.Property<string>("DataTypeName");

                    b.Property<DateTime>("DateCreated");

                    b.Property<string>("GeneratedReportId");

                    b.Property<int>("InvokeCallbackRetryCount");

                    b.Property<bool>("IsLocked");

                    b.Property<string>("LastAmazonReportProcessingStatus");

                    b.Property<DateTime>("LastAmazonRequestDate");

                    b.Property<string>("MerchantId");

                    b.Property<string>("MethodName");

                    b.Property<int>("ReportDownloadRetryCount");

                    b.Property<int>("ReportProcessRetryCount");

                    b.Property<string>("ReportRequestData");

                    b.Property<int>("ReportRequestRetryCount");

                    b.Property<string>("ReportType");

                    b.Property<string>("RequestReportId");

                    b.Property<string>("TypeName");

                    b.HasKey("Id");

                    b.HasIndex("RequestReportId", "GeneratedReportId");

                    b.ToTable("ReportRequestEntries");
                });

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.FeedSubmissionDetails", b =>
                {
                    b.HasOne("MountainWarehouse.EasyMWS.Data.FeedSubmissionEntry", "FeedSubmissionEntry")
                        .WithOne("Details")
                        .HasForeignKey("MountainWarehouse.EasyMWS.Data.FeedSubmissionDetails", "FeedSubmissionEntryId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.ReportRequestDetails", b =>
                {
                    b.HasOne("MountainWarehouse.EasyMWS.Data.ReportRequestEntry", "ReportRequestEntry")
                        .WithOne("Details")
                        .HasForeignKey("MountainWarehouse.EasyMWS.Data.ReportRequestDetails", "ReportRequestEntryId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
