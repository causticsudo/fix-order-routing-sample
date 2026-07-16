import React, { useState, useEffect } from 'react'
import './App.css'

class ErrorBoundary extends React.Component<any, any> {
  constructor(props: any) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: any) {
    return { hasError: true, error }
  }

  componentDidCatch(error: any, errorInfo: any) {
    console.error('Error caught by boundary:', error, errorInfo)
  }

  render() {
    if (this.state.hasError) {
      return (
        <div style={{ padding: '20px', color: 'red' }}>
          <h2>Algo deu errado!</h2>
          <p>{this.state.error?.message}</p>
          <button onClick={() => window.location.reload()}>Recarregar</button>
        </div>
      )
    }

    return this.props.children
  }
}

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

interface OrderEvent {
  eventId: string
  orderId: string
  correlationKey?: string
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

const SYMBOLS = ['PETR4', 'VALE3', 'VIIA4'] as const

const EVENT_LABELS: Record<string, string> = {
  created: 'Criado',
  submitted: 'Enviado',
  accepted: 'Aceito',
  rejected: 'Rejeitado',
}

function eventLabel(status: string): string {
  return EVENT_LABELS[status?.toLowerCase()] ?? status
}

function extractError(data: any): string | null {
  if (!data) return null
  if (Array.isArray(data.errors) && data.errors.length) return data.errors.join(', ')
  if (typeof data.error === 'string') return data.error
  if (typeof data === 'string') return data
  return null
}

async function parseResponse(res: Response, fallback: string) {
  const data = await res.json().catch(() => null)
  if (!res.ok) {
    throw new Error(extractError(data) || `${fallback} (HTTP ${res.status})`)
  }
  return data
}

async function getDebugToken(): Promise<string> {
  const res = await fetch(`${API_BASE}/token/debug`, { method: 'POST' })
  const data = await parseResponse(res, 'Falha ao obter token de autenticação')
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
  return parseResponse(res, 'Erro ao criar pedido')
}

async function getOrders(token: string) {
  const res = await fetch(`${API_BASE}/orders?page=1&pageSize=50`, {
    headers: { 'Authorization': `Bearer ${token}` },
  })
  const data = await parseResponse(res, 'Erro ao carregar pedidos')
  return {
    data: data.items || [],
  }
}

async function getOrderDetail(token: string, orderId: string) {
  const res = await fetch(`${API_BASE}/orders/${orderId}`, {
    headers: { 'Authorization': `Bearer ${token}` },
  })
  return parseResponse(res, 'Erro ao carregar detalhes do pedido')
}

async function getOrderEvents(token: string, orderId: string) {
  const res = await fetch(`${API_BASE}/orders/events?orderId=${orderId}&page=1&pageSize=100`, {
    headers: { 'Authorization': `Bearer ${token}` },
  })
  const data = await parseResponse(res, 'Erro ao carregar eventos do pedido')
  return {
    data: data.items || [],
  }
}

async function getExposure(token: string, symbol: string): Promise<Exposure> {
  const res = await fetch(`${API_BASE}/report/exposure?symbol=${symbol}`, {
    headers: { 'Authorization': `Bearer ${token}` },
  })
  const data = await parseResponse(res, 'Erro ao carregar exposure')
  const exposure = Array.isArray(data) ? data[0] : data
  return {
    symbol: exposure?.symbol ?? symbol,
    currentExposure: exposure?.currentExposure ?? 0,
    limitMin: exposure?.limitMin ?? -100_000_000,
    limitMax: exposure?.limitMax ?? 100_000_000,
  }
}

async function getHealth(): Promise<HealthStatus> {
  const res = await fetch(`${API_BASE}/health`)
  return parseResponse(res, 'Erro ao consultar health')
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
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null)
  const [orderEvents, setOrderEvents] = useState<OrderEvent[]>([])
  const [exposures, setExposures] = useState<Exposure[]>([])
  const [health, setHealth] = useState<HealthStatus | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  useEffect(() => {
    initToken()
  }, [])

  useEffect(() => {
    if (!token) return

    const interval = setInterval(() => {
      loadOrders(token)
      loadExposures(token)
      loadHealth()
    }, 5000)

    return () => clearInterval(interval)
  }, [token])

  useEffect(() => {
    if (token) {
      loadOrders(token)
      loadExposures(token)
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

  const loadExposures = async (t: string) => {
    try {
      const data = await Promise.all(SYMBOLS.map((sym) => getExposure(t, sym)))
      setExposures(data)
    } catch (e) {
      console.error('Failed to load exposures:', e)
      setExposures((prev) =>
        prev.length
          ? prev
          : SYMBOLS.map((sym) => ({
              symbol: sym,
              currentExposure: 0,
              limitMin: -100_000_000,
              limitMax: 100_000_000,
            }))
      )
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
    setFormError(null)

    const priceVal = Number(price)
    const qtyVal = Number(quantity)

    if (Number.isNaN(qtyVal) || qtyVal <= 0 || qtyVal > 99999) {
      setFormError('Quantidade deve ser um número entre 1 e 99999')
      return
    }
    if (Number.isNaN(priceVal) || priceVal <= 0 || priceVal > 999.99) {
      setFormError('Preço deve ser um número entre 0.01 e 999.99')
      return
    }
    if (Math.round(priceVal * 100) !== priceVal * 100) {
      setFormError('Preço deve ter no máximo 2 casas decimais')
      return
    }

    setLoading(true)
    try {
      const newOrder = await createOrder(token, {
        symbol,
        quantity: qtyVal,
        price: priceVal,
        side,
      })

      if (newOrder?.orderId) {
        setOrders([newOrder, ...orders])
      }
    } catch (e: any) {
      console.error('Failed to create order:', e)
      setFormError(e?.message || 'Erro ao criar pedido')
    } finally {
      setLoading(false)
    }
  }

  const handleOrderClick = async (order: Order) => {
    if (!token) return
    try {
      const detail = await getOrderDetail(token, order.orderId)
      const events = await getOrderEvents(token, order.orderId)
      const detailData = detail.data || detail || order
      const eventsData = Array.isArray(events) ? events : (events.data || [])
      setSelectedOrder(detailData)
      setOrderEvents(eventsData)
    } catch (e) {
      console.error('Failed to load order details:', e)
      alert('Erro ao carregar detalhes do pedido')
      setSelectedOrder(null)
    }
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
                  value={Number.isNaN(quantity) ? '' : quantity}
                  onChange={(e) => setQuantity(e.target.valueAsNumber)}
                  min="1"
                  max="99999"
                  step="1"
                />
              </div>

              <div className="form-group">
                <label>Preço</label>
                <input
                  type="number"
                  step="0.01"
                  value={Number.isNaN(price) ? '' : price}
                  onChange={(e) => setPrice(e.target.valueAsNumber)}
                  min="0.01"
                  max="999.99"
                />
              </div>

              {formError && <div className="form-error">{formError}</div>}

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
          <div className="exposure-grid">
            {exposures.map((exp) => {
              const usage = Math.min(100, (Math.abs(exp.currentExposure) / ((exp.limitMax - exp.limitMin) / 2)) * 100)
              return (
                <div key={exp.symbol} className="exposure-card">
                  <h2>{exp.symbol}</h2>
                  <div className="exposure-content">
                    <div className="metric">
                      <label>Current Exposure</label>
                      <div className="value">{exp.currentExposure.toFixed(2)}</div>
                    </div>
                    <div className="metric">
                      <label>Limit Range</label>
                      <div className="value">{exp.limitMin.toFixed(0)} to {exp.limitMax.toFixed(0)}</div>
                    </div>
                    <div className="metric">
                      <label>Usage</label>
                      <div className="usage-bar">
                        <div
                          className="usage-fill"
                          style={{ width: `${usage}%`, backgroundColor: getExposureColor(exp) }}
                        ></div>
                      </div>
                      <div className="usage-text">{usage.toFixed(1)}%</div>
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
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
                <div><strong>ID:</strong> {selectedOrder.orderId}</div>
                <div><strong>Symbol:</strong> {selectedOrder.symbol}</div>
                <div><strong>Side:</strong> {selectedOrder.side}</div>
                <div><strong>Quantity:</strong> {selectedOrder.quantity}</div>
                <div><strong>Price:</strong> R$ {selectedOrder.price.toFixed(2)}</div>
                <div><strong>Total:</strong> R$ {(selectedOrder.price * selectedOrder.quantity).toFixed(2)}</div>
                <div><strong>Status:</strong> {selectedOrder.status}</div>
                <div><strong>Created:</strong> {new Date(selectedOrder.createdAt).toLocaleString('pt-BR')}</div>
                {selectedOrder.rejectionReason && (
                  <div><strong>Rejection:</strong> {selectedOrder.rejectionReason}</div>
                )}
              </div>
              <div className="events-timeline">
                <h3>Histórico de Eventos ({orderEvents.length})</h3>
                {orderEvents.length === 0 ? (
                  <p className="empty">Nenhum evento registrado</p>
                ) : (
                  <div className="timeline">
                    {orderEvents.map((event) => (
                      <div key={event.eventId} className={`event event-${event.status.toLowerCase()}`}>
                        <div className="event-head">
                          <span className="event-status">{eventLabel(event.status)}</span>
                          <span className="event-time">{new Date(event.timestamp).toLocaleString('pt-BR')}</span>
                        </div>
                        {event.correlationKey && (
                          <div className="event-corr"><strong>ClOrdID:</strong> {event.correlationKey}</div>
                        )}
                        <div className="event-reason">{event.reason || '—'}</div>
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

const AppWithErrorBoundary = () => (
  <ErrorBoundary>
    <App />
  </ErrorBoundary>
)

export default AppWithErrorBoundary
