using System;
using CardDemo.Domain.Attributes;

namespace CardDemo.Domain.Models
{
    [Trace("cpy/CVTRA01Y.cpy", Ir = "dr:TCATBALF")]
    public record TransCatBalanceRecord(
        long AccountId,
        string TypeCd,
        int CatCd,
        decimal Balance
    );

    [Trace("cpy/CVACT01Y.cpy", Ir = "dr:ACCTFILE")]
    public record AccountRecord(
        long AccountId,
        string ActiveStatus,
        decimal CurrentBalance,
        decimal CreditLimit,
        decimal CashCreditLimit,
        string OpenDate,
        string ExpirationDate,
        string ReissueDate,
        decimal CurrCycCredit,
        decimal CurrCycDebit,
        string GroupId
    );

    [Trace("cpy/CVTRA02Y.cpy", Ir = "dr:DISCGRP")]
    public record DisclosureGroupRecord(
        string GroupId,
        string TypeCd,
        int CatCd,
        decimal InterestRate
    );

    [Trace("cpy/CVACT03Y.cpy", Ir = "dr:XREFFIL1")]
    public record CardXrefRecord(
        string CardNumber,
        int CustId,
        long AccountId
    );

    [Trace("cpy/CVTRA05Y.cpy", Ir = "dr:TRANSACT")]
    public record TransactionRecord(
        string TransId,
        string TypeCd,
        int CatCd,
        string Source,
        string Description,
        decimal Amount,
        string CardNumber,
        int MerchantId,
        string MerchantName,
        string MerchantCity,
        string MerchantZip,
        string OrigTs,
        string ProcTs
    );
}
