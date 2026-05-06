using System;

namespace Inventory.Application.DTOs.ReturnOrder
{
    public class ReturnOrderFilter
    {
        private int _page = 1;
        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 1 : value > 100 ? 100 : value;
        }

        public int? ProductId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
