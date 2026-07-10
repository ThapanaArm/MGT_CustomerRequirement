import type { CustomerRequirementForm, CustomerRequirementPage, StatusFilter } from "./types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5168";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), 8000);

  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...init?.headers
    },
    ...init,
    signal: controller.signal
  }).finally(() => {
    window.clearTimeout(timeout);
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(body || `Request failed with status ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function listCustomerRequirements(params: {
  search: string;
  status: StatusFilter;
  page?: number;
  pageSize?: number;
}) {
  const searchParams = new URLSearchParams({
    search: params.search,
    status: params.status,
    page: String(params.page ?? 1),
    pageSize: String(params.pageSize ?? 50)
  });

  return request<CustomerRequirementPage>(`/api/customer-requirements?${searchParams.toString()}`);
}

export function createCustomerRequirement(form: CustomerRequirementForm) {
  return request<void>("/api/customer-requirements", {
    method: "POST",
    body: JSON.stringify(form)
  });
}

export function updateCustomerRequirement(form: CustomerRequirementForm) {
  return request<void>(`/api/customer-requirements/${encodeURIComponent(form.customerCode_ECC6)}`, {
    method: "PUT",
    body: JSON.stringify(form)
  });
}

export function setCustomerRequirementActive(customerCode: string, isActive: boolean) {
  return request<void>(
    `/api/customer-requirements/${encodeURIComponent(customerCode)}/${isActive ? "active" : "inactive"}`,
    { method: "PATCH" }
  );
}

export function deleteCustomerRequirement(customerCode: string) {
  return request<void>(`/api/customer-requirements/${encodeURIComponent(customerCode)}`, {
    method: "DELETE"
  });
}
