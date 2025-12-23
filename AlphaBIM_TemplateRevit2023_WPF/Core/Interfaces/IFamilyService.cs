using System.Collections.Generic;
using System.Threading.Tasks;
using NTC.FamilyManager.Models;

namespace NTC.FamilyManager.Core.Interfaces
{
    public interface IFamilyService
    {
        Task<List<FamilyItem>> GetFamiliesAsync(string folderPath);
    }
}
