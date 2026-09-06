using PhotoAI.Core.Models;
using PhotoAI.Media.Processing;
using Xunit;

namespace PhotoAI.Core.Tests;

public class ImageProcessorTests
{
    [Fact]
    public void ComputeSimilarity_SameHash_Returns1()
    {
        ulong hash = 0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010;
        double similarity = ImageProcessor.ComputeSimilarity(hash, hash);
        Assert.Equal(1.0, similarity);
    }

    [Fact]
    public void ComputeSimilarity_DifferentHash_ReturnsLessThan1()
    {
        ulong hash1 = 0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010;
        ulong hash2 = 0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101;
        double similarity = ImageProcessor.ComputeSimilarity(hash1, hash2);
        Assert.Equal(0.0, similarity);
    }

    [Fact]
    public void ComputeSimilarity_OneBitDifferent()
    {
        ulong hash1 = 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000;
        ulong hash2 = 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000001;
        double similarity = ImageProcessor.ComputeSimilarity(hash1, hash2);
        Assert.Equal(63.0 / 64.0, similarity, 10);
    }

    [Fact]
    public void IsSupportedImageFile_Jpeg_ReturnsTrue()
    {
        var processor = new ImageProcessor();
        Assert.True(processor.IsSupportedImageFile("photo.jpg"));
        Assert.True(processor.IsSupportedImageFile("photo.JPEG"));
        Assert.True(processor.IsSupportedImageFile("photo.png"));
        Assert.True(processor.IsSupportedImageFile("photo.webp"));
    }

    [Fact]
    public void IsSupportedImageFile_Video_ReturnsFalse()
    {
        var processor = new ImageProcessor();
        Assert.False(processor.IsSupportedImageFile("video.mp4"));
        Assert.False(processor.IsSupportedImageFile("video.mov"));
    }

    [Fact]
    public void IsSupportedVideoFile_MP4_ReturnsTrue()
    {
        var processor = new ImageProcessor();
        Assert.True(processor.IsSupportedVideoFile("video.mp4"));
        Assert.True(processor.IsSupportedVideoFile("video.MOV"));
        Assert.True(processor.IsSupportedVideoFile("video.mkv"));
    }

    [Fact]
    public void IsSupportedVideoFile_Image_ReturnsFalse()
    {
        var processor = new ImageProcessor();
        Assert.False(processor.IsSupportedVideoFile("photo.jpg"));
        Assert.False(processor.IsSupportedVideoFile("photo.png"));
    }
}
