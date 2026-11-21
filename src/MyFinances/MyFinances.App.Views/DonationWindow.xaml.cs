using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using MyFinances.App.Infra;
using MyFinances.Data.Infra;
using MyFinances.Data.Services;
using QRCoder;

namespace MyFinances.App.Views;

public partial class DonationWindow : Window, IComponentConnector
{
	private sealed record LocalState(DateTime? LastTime, DateTime? SnoozeUntil);

	private readonly DonationInfo _info;

	public DonationWindow()
	{
		InitializeComponent();
		DarkTitleBar.Enable(this);
		_info = ReadInfo();
		TitleText.Text = _info.Title;
		MessageText.Text = _info.Message;
		bool hasPix = !string.IsNullOrWhiteSpace(_info.PixKey);
		bool hasLink = Uri.TryCreate(_info.Link, UriKind.Absolute, out Uri uri) && (uri.Scheme == "https" || uri.Scheme == "http");
		PixPanel.Visibility = ((!hasPix) ? Visibility.Collapsed : Visibility.Visible);
		PixText.Text = _info.PixKey ?? "";
		bool online = NetworkInterface.GetIsNetworkAvailable();
		LinkPanel.Visibility = ((!(hasLink & online)) ? Visibility.Collapsed : Visibility.Visible);
		OfflineText.Visibility = ((!hasLink || online) ? Visibility.Collapsed : Visibility.Visible);
		NoInfoPanel.Visibility = ((hasPix || hasLink) ? Visibility.Collapsed : Visibility.Visible);
		if (hasLink & online)
		{
			LinkText.Text = _info.Link;
			try
			{
				using QRCodeGenerator gen = new QRCodeGenerator();
				using QRCodeData date = gen.CreateQrCode(_info.Link, QRCodeGenerator.ECCLevel.M);
				byte[] png = new PngByteQRCode(date).GetGraphic(8);
				BitmapImage bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.StreamSource = new MemoryStream(png);
				bmp.EndInit();
				QrImage.Source = bmp;
			}
			catch (Exception ex)
			{
				Log.Error("Donation QR", ex);
				QrImage.Visibility = Visibility.Collapsed;
			}
		}
		Store(new LocalState(DateTime.Now, ReadState().SnoozeUntil));
	}

	private static DonationInfo ReadInfo()
	{
		try
		{
			string f = Path.Combine(AppContext.BaseDirectory, "donation.json");
			if (File.Exists(f))
			{
				return JsonSerializer.Deserialize<DonationInfo>(File.ReadAllText(f)) ?? new DonationInfo();
			}
		}
		catch (Exception ex)
		{
			Log.Error("Read donation.json", ex);
		}
		return new DonationInfo();
	}

	private static LocalState ReadState()
	{
		try
		{
			if (File.Exists(AppPaths.LocalConfig))
			{
				return JsonSerializer.Deserialize<LocalState>(File.ReadAllText(AppPaths.LocalConfig)) ?? new LocalState(null, null);
			}
		}
		catch
		{
		}
		return new LocalState(null, null);
	}

	private static void Store(LocalState e)
	{
		try
		{
			File.WriteAllText(AppPaths.LocalConfig, JsonSerializer.Serialize(e));
		}
		catch
		{
		}
	}

	public static bool ShouldShow()
	{
		LocalState e = ReadState();
		DateTime? snoozeUntil = e.SnoozeUntil;
		if (snoozeUntil.HasValue)
		{
			DateTime a = snoozeUntil.GetValueOrDefault();
			if (a > DateTime.Now)
			{
				return false;
			}
		}
		if (e.LastTime.HasValue)
		{
			return e.LastTime < DateTime.Now.AddDays(-7.0);
		}
		return true;
	}

	private void CopyPix_Click(object sender, RoutedEventArgs e)
	{
		Clipboard.SetText(PixText.Text);
	}

	private void OpenLink_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo(_info.Link)
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	private void Snooze_Click(object sender, RoutedEventArgs e)
	{
		Store(new LocalState(DateTime.Now, DateTime.Now.AddDays(30.0)));
		Close();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
