export interface Order {
  orderId: string;
  symbol: string;
  side: 'BUY' | 'SELL';
  quantity: number;
  price: number;
  status: 'Submitted' | 'Accepted' | 'Rejected' | 'Executed';
  createdAt: string;
  rejectionReason?: string;
}

export interface CreateOrderRequest {
  symbol: string;
  quantity: number;
  price: number;
  side: 'BUY' | 'SELL';
  orderType: 'LIMIT' | 'MARKET';
  timeInForce: 'GTC' | 'IOC' | 'FOK';
}
