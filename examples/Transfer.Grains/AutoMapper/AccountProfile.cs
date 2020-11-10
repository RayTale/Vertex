using AutoMapper;
using Transfer.Grains.Snapshot;
using Transfer.Repository.Entities;

namespace Transfer.Grains.AutoMapper
{
    public class AccountProfile : Profile
    {
        public AccountProfile()
        {
            this.CreateMap<Account, AccountSnapshot>();
        }
    }
}
