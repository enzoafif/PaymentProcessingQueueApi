using System.ComponentModel.DataAnnotations;

namespace PaymentProcessingQueueApi.Api.Validation;

/// <summary>
/// Valida que a string é um CPF (11 dígitos) ou CNPJ (14 dígitos) válido — incluindo os
/// DÍGITOS VERIFICADORES, não apenas o formato. Aceita somente números (sem máscara).
/// Valores nulos/vazios são tratados como válidos aqui: a obrigatoriedade fica a cargo
/// do atributo <c>[Required]</c> (evita mensagens de erro duplicadas).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class CpfCnpjAttribute : ValidationAttribute
{
    public CpfCnpjAttribute()
        : base("Informe um CPF (11 dígitos) ou CNPJ (14 dígitos) válido, apenas números.") { }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;             // [Required] cuida do nulo
        if (value is not string text) return false;
        if (text.Length == 0) return true;          // idem para vazio

        // Aceita apenas dígitos (sem máscara/pontuação).
        foreach (var c in text)
            if (!char.IsDigit(c)) return false;

        return text.Length switch
        {
            11 => IsValidCpf(text),
            14 => IsValidCnpj(text),
            _ => false
        };
    }

    private static bool IsValidCpf(string cpf)
    {
        if (AllSameDigit(cpf)) return false; // ex.: 00000000000, 11111111111

        var d1 = Mod11(cpf, length: 9, startWeight: 10);
        var d2 = Mod11(cpf, length: 10, startWeight: 11);
        return d1 == Digit(cpf, 9) && d2 == Digit(cpf, 10);
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (AllSameDigit(cnpj)) return false;

        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var d1 = Mod11Weighted(cnpj, 12, weights1);
        var d2 = Mod11Weighted(cnpj, 13, weights2);
        return d1 == Digit(cnpj, 12) && d2 == Digit(cnpj, 13);
    }

    // CPF: pesos decrescentes a partir de startWeight (10 para o 1º dígito, 11 para o 2º).
    private static int Mod11(string digits, int length, int startWeight)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += Digit(digits, i) * (startWeight - i);
        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }

    // CNPJ: pesos fornecidos por tabela.
    private static int Mod11Weighted(string digits, int length, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += Digit(digits, i) * weights[i];
        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }

    private static int Digit(string s, int index) => s[index] - '0';

    private static bool AllSameDigit(string s)
    {
        for (var i = 1; i < s.Length; i++)
            if (s[i] != s[0]) return false;
        return true;
    }
}
