const API_BASE = 'http://localhost:5000/api/v1';

export async function getDebugToken(): Promise<string> {
  const res = await fetch(`${API_BASE}/token/debug`, { method: 'POST' });
  const data = await res.json();
  return data.token;
}

export async function createOrder(token: string, order: any) {
  const res = await fetch(`${API_BASE}/orders`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(order),
  });
  return res.json();
}

export async function getOrders(token: string) {
  const res = await fetch(`${API_BASE}/orders`, {
    headers: { 'Authorization': `Bearer ${token}` },
  });
  return res.json();
}
