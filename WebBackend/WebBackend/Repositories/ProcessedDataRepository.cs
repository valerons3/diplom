using WebBackend.Data;
using WebBackend.Models.Entity;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class ProcessedDataRepository : IProcessedDataRepository
    {
        private readonly AppDbContext context;
        public ProcessedDataRepository(AppDbContext context)
        {
            this.context = context;
        }
        public async Task<(bool Sucess, string? message)> PostProcessDataAsync(ProcessedData processedData)
        {
            try
            {
                await context.ProccesedDatas.AddAsync(processedData);
                await context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, "Ошибка при сохранении данных в базу данных");
            }
        }
    }
}
