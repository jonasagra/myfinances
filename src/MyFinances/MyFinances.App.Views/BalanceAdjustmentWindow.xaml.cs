using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using MyFinances.App.Infra;
using MyFinances.Core.Common;
using MyFinances.Core.Finance;

namespace MyFinances.App.Views;

public partial class BalanceAdjustmentWindow : Window, IComponentConnector
{
	private readonly decimal _remaining;

	public decimal BalanceEntered { get; private set; }

	public decimal PendingInflow { get; private set; }

	public decimal PendingOutflow { get; private set; }

	public BalanceAdjustmentWindow(string monthName, decimal remainingWithoutAdjustment)
	{
		InitializeComponent();
		DarkTitleBar.Enable(this);
		_remaining = remainingWithoutAdjustment;
		Header.Text = $"Pelo que está lançado, o app acha que você termina {monthName} com {Money.Fmt(remainingWithoutAdjustment)}. " + "Diga quanto tem de verdade e o app lança a diferença como um ajuste (se já houver um ajuste neste mês, ele é trocado).";
		Loaded += (object _, RoutedEventArgs _) =>
		{
			RealBalanceBox.Focus();
		};
		Refresh();
	}

	private void Field_TextChanged(object sender, TextChangedEventArgs e)
	{
		Refresh();
	}

	private bool Read(out decimal real, out decimal incoming, out decimal outgoing)
	{
		incoming = (outgoing = 0m);
		if ((Money.TryParse(RealBalanceBox?.Text, out real) & (string.IsNullOrWhiteSpace(PendingInflowBox?.Text) || Money.TryParse(PendingInflowBox.Text, out incoming)) & (string.IsNullOrWhiteSpace(PendingOutflowBox?.Text) || Money.TryParse(PendingOutflowBox.Text, out outgoing))) && incoming >= 0m)
		{
			return outgoing >= 0m;
		}
		return false;
	}

	private void Refresh()
	{
		if (Preview != null && Apply != null)
		{
			if (!Read(out var real, out var incoming, out var outgoing))
			{
				Preview.Text = "Digite os valores (ex.: 295,90).";
				Apply.IsEnabled = false;
				return;
			}
			decimal diff = FinanceSummary.RealBalanceDifference(_remaining, real, incoming, outgoing);
			decimal expected = _remaining - incoming + outgoing;
			TextBox preview = Preview;
			string text = $"Restante lançado no mês:        {Money.Fmt(_remaining)}\n(−) ainda vai entrar:           {Money.Fmt(incoming)}\n(+) ainda vai sair:             {Money.Fmt(outgoing)}\n= deveria ter hoje:             {Money.Fmt(expected)}\nVocê tem:                       {Money.Fmt(real)}\n";
			string? text2;
			if (diff == 0m)
			{
				text2 = "Bate certinho. Nenhum ajuste (e o ajuste antigo, se houver, sai).";
			}
			else
			{
				text2 = ((diff > 0m) ? ("Faltam " + Money.Fmt(diff) + " → lança uma DESPESA de ajuste.") : ("Sobram " + Money.Fmt(-diff) + " → lança uma ENTRADA de ajuste."));
			}
			preview.Text = text + text2;
			Apply.IsEnabled = true;
		}
	}

	private void Apply_Click(object sender, RoutedEventArgs e)
	{
		if (Read(out var real, out var incoming, out var outgoing))
		{
			BalanceEntered = real;
			PendingInflow = incoming;
			PendingOutflow = outgoing;
			DialogResult = true;
		}
	}
}
