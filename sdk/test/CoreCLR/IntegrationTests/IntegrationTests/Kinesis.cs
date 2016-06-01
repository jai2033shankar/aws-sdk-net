using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Runtime;
using Xunit;
using Amazon.DNXCore.IntegrationTests;


namespace Amazon.DNXCore.IntegrationTests
{
    
    public class Kinesis : TestBase<AmazonKinesisClient>
    {
        protected override void Dispose(bool disposing)
        {
            // Delete all dotnet integ test streams.
            var streamNames = Client.ListStreamsAsync().Result.StreamNames;
            foreach (var streamName in streamNames)
            {
                if (streamName.Contains("dotnet-integ-test-stream"))
                {
                    try
                    {
                        Client.DeleteStreamAsync(new DeleteStreamRequest
                        {
                            StreamName = streamName
                        }).Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to delete stream {0}: {1}", streamName, e.Message);
                    }
                }
            }
            base.Dispose(disposing);
        }
        
        [Fact]
        [Trait(CategoryAttribute,"Kinesis")]
        public async Task KinesisCRUD()
        {
            var streamName = "dotnet-integ-test-stream-" + DateTime.Now.Ticks;

            // Create a stream.
            await Client.CreateStreamAsync(new CreateStreamRequest
            {
                ShardCount = 1,
                StreamName = streamName
            });

            // Describe the stream.
            var stream = (await Client.DescribeStreamAsync(new DescribeStreamRequest
            {
                StreamName = streamName
            })).StreamDescription;
            Assert.Equal(stream.HasMoreShards, false);
            Assert.False(string.IsNullOrEmpty(stream.StreamARN));
            Assert.Equal(stream.StreamName, streamName);
            Assert.True(stream.StreamStatus == StreamStatus.CREATING);

            // List streams.
            var streamNames = (await Client.ListStreamsAsync()).StreamNames;
            Assert.True(streamNames.Count > 0);
            Assert.True(streamNames.Contains(streamName));

            // Delete the stream.
            await Client.DeleteStreamAsync(new DeleteStreamRequest
            {
                StreamName = streamName
            });
            stream = (await Client.DescribeStreamAsync(new DescribeStreamRequest
            {
                StreamName = streamName
            })).StreamDescription;
            Assert.True(stream.StreamStatus == StreamStatus.DELETING);
        }

        private StreamDescription WaitForStreamToBeActive(string streamName)
        {
            while (true)
            {
                var stream = Client.DescribeStreamAsync(new DescribeStreamRequest
                {
                    StreamName = streamName
                }).Result.StreamDescription;

                if (stream.StreamStatus != StreamStatus.ACTIVE)
                {
                    UtilityMethods.Sleep(TimeSpan.FromSeconds(5));
                    continue;
                }
                else
                {
                    return stream;
                }
            }
        }
    }
}