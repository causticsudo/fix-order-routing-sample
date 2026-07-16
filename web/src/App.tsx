import { useState, useEffect } from 'react'
import './App.css'

interface Order {
  orderId: string
  symbol: string
  side: 'BUY' | 'SELL'
  quantity: number
  price: number
  status: string
  createdAt: string
  rejectionReason?: string
}

interface OrderDetail extends Order {
  orderType: string
  timeInForce: string
}

interface OrderEvent {
  eventId: string
  orderId: string
  status: string
  timestamp: string
  reason?: string
}

interface Exposure {
  symbol: string
  currentExposure: number
  limitMin: number
  limitMax: number
}

interface HealthStatus {
  status: string
  database: string
  cache: string
}

const API_BASE = 'http://localhost:5000/api/v1'

async function getDebugToken(): Promise<string> {
  const res = await fetch(`${API_BASE}/token/debug`, { method: 'POST' })
  const data = await res.json()
  return data.token
}

async function createOrder(token: string, order: any) {
  const res = await fetch(`${API_BASE}/orders`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(order),
  })
  return res.json()
}

async function getOrders(token: string) {
  const res = await fetch(`${API_BASE}/orders`, {
    headers: { 'Authorization': `Bearer ${token}` },
  })
  return res.json()
}

async function getOrderDetail(token: string, orderId: string) {
  const res = await fetch(`${API_BASE}/orders/${orderId}`, {
    headers: { 'Authorization': `Bearer ${token}` },
  })
  return res.json()
}

async function getOrderEvents(token: string, orderId: string) {
  const res = await fetch(`${API_BASE}/orders/events?orderId=${orderId}`, {
    headers: { 'Authorization': `Bearer ${token}` },
  })
  return res.json()
}

async function getExposure(token: string, symbol: string): Promise<Exposure> {
  const res = await fetch(`${API_BASE}/reports/exposure?symbol=${symbol}`, {
    headers: { 'Authorization': `Bearer ${token}` },
  })
  const data = await res.json()
  return data.data
}

async function getHealth(): Promise<HealthStatus> {
  const res = await fetch(`${API_BASE}/health`)
  return res.json()
}

function App() {
  const [token, setToken] = useState<string>('')
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(false)
  const [symbol, setSymbol] = useState('PETR4')
  const [quantity, setQuantity] = useState(100)
  const [price, setPrice] = useState(25.5)
  const [side, setSide] = useState<'BUY' | 'SELL'>('BUY')
  const [currentView, setCurrentView] = useState<'orders' | 'exposure'>('orders')
  const [selectedOrder, setSelectedOrder] = useState<OrderDetail | null>(null)
  const [orderEvents, setOrderEvents] = useState<OrderEvent[]>([])
  const [exposure, setExposure] = useState<Exposure | null>(null)
  const [exposureSymbol, setExposureSymbol] = useState('PETR4')
  const [health, setHealth] = useState<HealthStatus | null>(null)

  useEffect(() => {
    initToken()
  }, [])

  useEffect(() => {
    if (!token) return

    const interval = setInterval(() => {
      loadOrders(token)
      loadExposure(token, exposureSymbol)
      loadHealth()
    }, 5000)

    return () => clearInterval(interval)
  }, [token, exposureSymbol])

  useEffect(() => {
    if (token) {
      loadOrders(token)
      loadExposure(token, exposureSymbol)
      loadHealth()
    }
  }, [token])

  const initToken = async () => {
    try {
      const t = await getDebugToken()
      setToken(t)
    } catch (e) {
      console.error('Failed to get token:', e)
    }
  }

  const loadOrders = async (t: string) => {
    try {
      const data = await getOrders(t)
      setOrders(data.data || [])
    } catch (e) {
      console.error('Failed to load orders:', e)
    }
  }

  const loadExposure = async (t: string, sym: string) => {
    try {
      const data = await getExposure(t, sym)
      setExposure(data)
    } catch (e) {
      console.error('Failed to load exposure:', e)
    }
  }

  const loadHealth = async () => {
    try {
      const data = await getHealth()
      setHealth(data)
    } catch (e) {
      console.error('Failed to load health:', e)
    }
  }

  const handleCreateOrder = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!token) return

    setLoading(true)
    try {
      const newOrder = await createOrder(token, {
        symbol,
        quantity: parseInt(quantity.toString()),
        price: parseFloat(price.toString()),
        side,
        orderType: 'LIMIT',
        timeInForce: 'GTC',
      })

      setOrders([...orders, newOrder])
      setQuantity(100)
      setPrice(25.5)
    } catch (e) {
      console.error('Failed to create order:', e)
    } finally {
      setLoading(false)
    }
  }

  const handleOrderClick = async (order: Order) => {
    if (!token) return
    try {
      const detail = await getOrderDetail(token, order.orderId)
      const events = await getOrderEvents(token, order.orderId)
      setSelectedOrder(detail.data || detail)
      setOrderEvents(events.data || [])
    } catch (e) {
      console.error('Failed to load order details:', e)
    }
  }

  const handleSymbolChange = (sym: string) => {
    setExposureSymbol(sym)
  }

  const getExposureColor = (exposure: Exposure) => {
    const limit = exposure.limitMax - exposure.limitMin
    const usage = Math.abs(exposure.currentExposure) / (limit / 2)
    if (usage < 0.5) return '#10b981'
    if (usage < 0.8) return '#f59e0b'
    return '#ef4444'
  }

  return (
    <div className="app">
      <div className="header">
        <h1>Fix Order Routing</h1>
        {health && (
          <div className="health-badge">
            <div className={`health-dot ${health.database === 'Connected' ? 'ok' : 'error'}`} title={`DB: ${health.database}`}></div>
            <div className={`health-dot ${health.cache === 'Connected' ? 'ok' : 'error'}`} title={`Cache: ${health.cache}`}></div>
          </div>
        )}
      </div>

      <div className="nav-tabs">
        <button
          className={`tab ${currentView === 'orders' ? 'active' : ''}`}
          onClick={() => setCurrentView('orders')}
        >
          Pedidos
        </button>
        <button
          className={`tab ${currentView === 'exposure' ? 'active' : ''}`}
          onClick={() => setCurrentView('exposure')}
        >
          Exposure
        </button>
      </div>

      {currentView === 'orders' && (
        <div className="container">
          <div className="form-section">
            <h2>Criar Pedido</h2>
            <form onSubmit={handleCreateOrder}>
              <div className="form-group">
                <label>Símbolo</label>
                <select value={symbol} onChange={(e) => setSymbol(e.target.value)}>
                  <option>PETR4</option>
                  <option>VALE3</option>
                  <option>VIIA4</option>
                </select>
              </div>

              <div className="form-group">
                <label>Lado</label>
                <select value={side} onChange={(e) => setSide(e.target.value as 'BUY' | 'SELL')}>
                  <option value="BUY">BUY</option>
                  <option value="SELL">SELL</option>
                </select>
              </div>

              <div className="form-group">
                <label>Quantidade</label>
                <input
                  type="number"
                  value={quantity}
                  onChange={(e) => setQuantity(parseInt(e.target.value))}
                  min="1"
                />
              </div>

              <div className="form-group">
                <label>Preço</label>
                <input
                  type="number"
                  step="0.01"
                  value={price}
                  onChange={(e) => setPrice(parseFloat(e.target.value))}
                  min="0"
                />
              </div>

              <button type="submit" disabled={loading}>
                {loading ? 'Enviando...' : 'Criar Pedido'}
              </button>
            </form>
          </div>

          <div className="orders-section">
            <h2>Pedidos ({orders.length})</h2>
            {orders.length === 0 ? (
              <p className="empty">Nenhum pedido criado ainda</p>
            ) : (
              <div className="orders-list">
                {orders.map((order) => (
                  <div
                    key={order.orderId}
                    className={`order-card status-${order.status.toLowerCase()}`}
                    onClick={() => handleOrderClick(order)}
                    style={{ cursor: 'pointer' }}
                  >
                    <div className="order-header">
                      <span className="symbol">{order.symbol}</span>
                      <span className={`side ${order.side.toLowerCase()}`}>{order.side}</span>
                      <span className="status">{order.status}</span>
                    </div>
                    <div className="order-details">
                      <p><strong>Qtd:</strong> {order.quantity}</p>
                      <p><strong>Preço:</strong> R$ {order.price.toFixed(2)}</p>
                      <p><strong>ID:</strong> {order.orderId.slice(0, 8)}...</p>
                    </div>
                    {order.rejectionReason && (
                      <div className="rejection">
                        {order.rejectionReason}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {currentView === 'exposure' && (
        <div className="exposure-view">
          <div className="exposure-symbols">
            {['PETR4', 'VALE3', 'VIIA4'].map((sym) => (
              <button
                key={sym}
                className={`symbol-tab ${exposureSymbol === sym ? 'active' : ''}`}
                onClick={() => handleSymbolChange(sym)}
              >
                {sym}
              </button>
            ))}
          </div>

          {exposure && (
            <div className="exposure-card">
              <h2>{exposure.symbol} Exposure Dashboard</h2>
              <div className="exposure-content">
                <div className="metric">
                  <label>Current Exposure</label>
                  <div className="value">{exposure.currentExposure.toFixed(2)}</div>
                </div>
                <div className="metric">
                  <label>Limit Range</label>
                  <div className="value">{exposure.limitMin.toFixed(0)} to {exposure.limitMax.toFixed(0)}</div>
                </div>
                <div className="metric">
                  <label>Usage</label>
                  <div className="usage-bar">
                    <div
                      className="usage-fill"
                      style={{
                        width: `${Math.min(100, (Math.abs(exposure.currentExposure) / ((exposure.limitMax - exposure.limitMin) / 2)) * 100)}%`,
                        backgroundColor: getExposureColor(exposure)
                      }}
                    ></div>
                  </div>
                  <div className="usage-text">
                    {((Math.abs(exposure.currentExposure) / ((exposure.limitMax - exposure.limitMin) / 2)) * 100).toFixed(1)}%
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {selectedOrder && (
        <div className="modal-overlay" onClick={() => setSelectedOrder(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{selectedOrder.symbol} - {selectedOrder.orderId.slice(0, 8)}...</h2>
              <button className="close" onClick={() => setSelectedOrder(null)}>x</button>
            </div>
            <div className="modal-content">
              <div className="order-info">
                <div><strong>Symbol:</strong> {selectedOrder.symbol}</div>
                <div><strong>Side:</strong> {selectedOrder.side}</div>
                <div><strong>Quantity:</strong> {selectedOrder.quantity}</div>
                <div><strong>Price:</strong> R$ {selectedOrder.price.toFixed(2)}</div>
                <div><strong>Status:</strong> {selectedOrder.status}</div>
                <div><strong>Type:</strong> {selectedOrder.orderType}</div>
                <div><strong>TIF:</strong> {selectedOrder.timeInForce}</div>
                <div><strong>Created:</strong> {new Date(selectedOrder.createdAt).toLocaleString('pt-BR')}</div>
                {selectedOrder.rejectionReason && (
                  <div><strong>Rejection:</strong> {selectedOrder.rejectionReason}</div>
                )}
              </div>
              <div className="events-timeline">
                <h3>Events</h3>
                {orderEvents.length === 0 ? (
                  <p className="empty">No events</p>
                ) : (
                  <div className="timeline">
                    {orderEvents.map((event) => (
                      <div key={event.eventId} className="event">
                        <div className="event-status">{event.status}</div>
                        <div className="event-time">{new Date(event.timestamp).toLocaleTimeString('pt-BR')}</div>
                        {event.reason && <div className="event-reason">{event.reason}</div>}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default App
