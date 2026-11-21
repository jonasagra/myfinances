namespace MyFinances.Core.Labor;

public sealed record LegalParameters(int Year, decimal MinimumWage, string MinimumWageSource, InssTable Inss, IrrfTable Irrf, UnemploymentBenefitTable UnemploymentBenefit, decimal FgtsPct, decimal FgtsPenaltyPct, decimal FgtsPenaltyAgreementPct, decimal PayrollLoanMaxSeverancePct, decimal PayrollLoanFgtsBalancePct, decimal PayrollLoanFgtsPenaltyPct, decimal PayrollLoanMarginPct)
{
	public static LegalParameters Default2026 { get; } = new LegalParameters(2026, 1621.00m, "1ª faixa do INSS 2026 (gov.br/inss, Portaria Interministerial MPS/MF nº 13/2026) = salário mínimo", new InssTable(2026, new _003C_003Ez__ReadOnlyArray<InssBand>(new InssBand[4]
	{
		new InssBand(1621.00m, 7.5m),
		new InssBand(2902.84m, 9m),
		new InssBand(4354.27m, 12m),
		new InssBand(8475.55m, 14m)
	}), "gov.br/inss — Tabela de contribuição mensal; Portaria Interministerial MPS/MF nº 13, de 09/01/2026", Confirmed: true), new IrrfTable(2026, new _003C_003Ez__ReadOnlyArray<IrrfBand>(new IrrfBand[5]
	{
		new IrrfBand(2428.80m, 0m, 0m),
		new IrrfBand(2826.65m, 7.5m, 182.16m),
		new IrrfBand(3751.05m, 15m, 394.16m),
		new IrrfBand(4664.68m, 22.5m, 675.49m),
		new IrrfBand(decimal.MaxValue, 27.5m, 908.73m)
	}), 189.59m, 607.20m, 5000.00m, 312.89m, 7350.00m, 978.62m, 0.133145m, "gov.br/receitafederal — Tabelas 2026; redução da Lei 15.270/2025", Confirmed: true), new UnemploymentBenefitTable(2026, 2222.17m, 3703.99m, 1777.74m, 2518.65m, "Valores usados na planilha; coincidem com o reajuste do MTE divulgado em 01/2026 (página oficial de serviço desatualizada)", Confirmed: false), 8m, 40m, 20m, 35m, 10m, 100m, 35m);
}
