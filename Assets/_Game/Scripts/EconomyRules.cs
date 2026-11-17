using System;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public class StockDefinition
    {
        public string StockId;
        public string CompanyName;
        public string Sector;
        public int BasePrice = 1000;

        [Range(0, .2f)]
        public float Volatility = .025f;

        [Range(0, 2)]
        public float NewsSensitivity = 1;
    }

    public enum EconomicNewsKind
    {
        General,
        CompanyGood,
        CompanyBad,
        Sector,
        Market,
        Satire,
    }

    [Serializable]
    public class EconomicNewsDefinition
    {
        public string NewsId;
        public string Title;

        [TextArea]
        public string Description;
        public EconomicNewsKind Kind;
        public string[] AffectedStocks = new string[0];
        public string Sector;
        public int MinimumDay = 1;
        public int MinimumCredit;

        [Range(-.2f, .2f)]
        public float PriceImpact = .025f;

        [Min(1)]
        public int Duration = 2;

        [Range(0, 1)]
        public float Probability = .08f;
    }

    [Serializable]
    public class SalaryTaxRule
    {
        public string jobId;

        [Range(0, 5000)]
        public int basisPoints = 1000;
    }

    [CreateAssetMenu(menuName = "Ian/Economy Rules")]
    public class EconomyRules : ScriptableObject
    {
        [Min(1)]
        public int taxPeriodDays = 7;

        [Min(1)]
        public int taxGraceDays = 3;

        [Min(1)]
        public int debtPeriodDays = 7;

        [Range(0, 5000)]
        public int salaryTaxBasisPoints = 1000;

        [Range(0, 5000)]
        public int lateTaxBasisPoints = 200;

        [Range(0, 5000)]
        public int debtInterestBasisPoints = 100;

        [Min(0)]
        public int minimumDebtPayment = 100;

        [Min(0)]
        public int repaymentCredit = 1;

        [Min(0)]
        public int overdueCreditPenalty = 2;

        [Range(0, 100)]
        public int minimumTradingCredit = 20;

        [Min(1)]
        public int marketIntervalSeconds = 300;

        [Min(1)]
        public int maximumCatchupTicks = 288;

        [Min(1)]
        public int historyLimit = 96;
        public SalaryTaxRule[] salaryTaxes = new SalaryTaxRule[0];
        public string incomeMessage = "축하합니다. 이안의 소득이 증가했습니다. 정부도 기뻐하고 있습니다.";
        public string taxPaidMessage = "세금 납부가 완료되었습니다. 행복도는 과세 대상이 아닙니다. 아직은요.";
        public StockDefinition[] stocks =
        {
            new StockDefinition
            {
                StockId = "IAN",
                CompanyName = "이안전자",
                Sector = "전자제품",
                BasePrice = 1000,
            },
            new StockDefinition
            {
                StockId = "SOC",
                CompanyName = "사회성홀딩스",
                Sector = "서비스",
                BasePrice = 800,
            },
            new StockDefinition
            {
                StockId = "BIO",
                CompanyName = "멘탈바이오",
                Sector = "바이오",
                BasePrice = 600,
                Volatility = .05f,
            },
            new StockDefinition
            {
                StockId = "WORK",
                CompanyName = "야근산업",
                Sector = "제조",
                BasePrice = 1200,
            },
            new StockDefinition
            {
                StockId = "WATER",
                CompanyName = "물값에너지",
                Sector = "에너지",
                BasePrice = 900,
            },
            new StockDefinition
            {
                StockId = "PHONE",
                CompanyName = "헤드폰테크",
                Sector = "음향기기",
                BasePrice = 1500,
            },
        };
        public EconomicNewsDefinition[] news =
        {
            new EconomicNewsDefinition
            {
                NewsId = "port",
                Title = "헤드폰테크, 충전 단자를 2mm 이동한 신제품 발표",
                Description = "시장은 이를 혁신으로 평가했습니다.",
                Kind = EconomicNewsKind.CompanyGood,
                AffectedStocks = new[] { "PHONE" },
                PriceImpact = .04f,
            },
            new EconomicNewsDefinition
            {
                NewsId = "home",
                Title = "야근산업, 직원들의 정시 퇴근 발표",
                Description = "시장에서는 생산성 위기를 우려하고 있습니다.",
                Kind = EconomicNewsKind.CompanyBad,
                AffectedStocks = new[] { "WORK" },
                PriceImpact = -.04f,
            },
            new EconomicNewsDefinition
            {
                NewsId = "macro",
                Title = "경기 회복 전망 발표",
                Description = "전망은 무료입니다. 생활비는 아닙니다.",
                Kind = EconomicNewsKind.General,
                PriceImpact = .01f,
                Probability = .03f,
            },
            new EconomicNewsDefinition
            {
                NewsId = "bio",
                Title = "멘탈바이오, 월요일 치료제 임상 지연",
                Description = "금요일까지 기다리라는 처방입니다.",
                Kind = EconomicNewsKind.Sector,
                Sector = "바이오",
                PriceImpact = -.025f,
            },
            new EconomicNewsDefinition
            {
                NewsId = "market",
                Title = "시장 전체 긴축",
                Description = "지갑은 이미 긴축 중이었습니다.",
                Kind = EconomicNewsKind.Market,
                PriceImpact = -.015f,
                Probability = .03f,
            },
            new EconomicNewsDefinition
            {
                NewsId = "satire",
                Title = "행복세 도입 부인",
                Description = "아직은 과세 대상이 아닙니다.",
                Kind = EconomicNewsKind.Satire,
                PriceImpact = .005f,
                Probability = .03f,
            },
        };

        public int TaxRate(string job) =>
            Array.Find(salaryTaxes, r => r != null && r.jobId == job)?.basisPoints ?? salaryTaxBasisPoints;
    }
}
