namespace MyFinances.App.ViewModels;

public sealed record SeriesBar(string Month, double ExpectedHeight, double ReceivedHeight, decimal Expected, decimal Received);
