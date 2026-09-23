using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

[Table("Monitoring")]
public class Monitoring : BaseEntity
{
    public Guid ViagemId { get; set; } // FK
    public decimal UltimoProgressoPorcentual { get; set; }
    public DateTimeOffset? UltimoEtaCalculado { get; set; }
    public DateTimeOffset? UltimaAtualizacaoPing { get; set; }

    // Navegação do EF Core
    public Travel Viagem { get; set; }

    public Monitoring(Guid viagemId)
    {
        if (viagemId == Guid.Empty) throw new ArgumentException("ViagemId é obrigatório");
        
        ViagemId = viagemId;
        UltimoProgressoPorcentual = 0;
    }

    // Verificar se implementação será na camada de domínio
    // Atualiza o cache quando a regra de negócio determina que estourou os 15 minutos
    public void AtualizarCache(decimal progresso, DateTimeOffset novoEta)
    {
        if (progresso < 0 || progresso > 100) 
            throw new ArgumentException("O progresso deve estar entre 0 e 100");

        UltimoProgressoPorcentual = progresso;
        UltimoEtaCalculado = novoEta;
        UltimaAtualizacaoPing = DateTimeOffset.UtcNow;
    }
}