using AutoMapper;
using DigiMenuAPI.Infrastructure.SQL;

namespace DigiMenuAPI.Application.Services
{
    public class SocialLinkService
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly LogMessageDispatcher<SocialLinkService> logger;

        public SocialLinkService(ApplicationDbContext context, IMapper mapper, LogMessageDispatcher<SocialLinkService> logger)
        {
            this.context = context;
            this.mapper = mapper;
            this.logger = logger;
        }
    }
}
