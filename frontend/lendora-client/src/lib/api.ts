export type ApiSuccess<T> = {
  success: true;
  message: string;
  data: T;
  traceId?: string;
};

export type ApiFailure = {
  success: false;
  message: string;
  statusCode: number;
  errors: string[];
  traceId?: string;
};

export class ApiClientError extends Error {
  statusCode: number;
  errors: string[];

  constructor(message: string, statusCode: number, errors: string[] = []) {
    super(message);
    this.name = "ApiClientError";
    this.statusCode = statusCode;
    this.errors = errors;
  }
}

export type ApiRequestOptions = RequestInit & {
  accessToken?: string | null;
};

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5081/api";

export async function apiRequest<T>(
  path: string,
  options: ApiRequestOptions = {}
): Promise<ApiSuccess<T>> {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  const hasBody = options.body !== undefined;
  const isFormData = typeof FormData !== "undefined" && options.body instanceof FormData;
  if (hasBody && !isFormData && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (options.accessToken) {
    headers.set("Authorization", `Bearer ${options.accessToken}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers
  });

  const text = await response.text();
  const payload = text ? (JSON.parse(text) as ApiSuccess<T> | ApiFailure) : null;

  if (!response.ok || !payload?.success) {
    const errorPayload = payload as ApiFailure | null;
    throw new ApiClientError(
      errorPayload?.message ?? `Request failed with status ${response.status}.`,
      errorPayload?.statusCode ?? response.status,
      errorPayload?.errors ?? []
    );
  }

  return payload;
}

export async function apiRequestBlob(
  path: string,
  options: ApiRequestOptions = {}
): Promise<Blob> {
  const headers = new Headers(options.headers);

  if (options.accessToken) {
    headers.set("Authorization", `Bearer ${options.accessToken}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers
  });

  if (!response.ok) {
    const text = await response.text();
    const payload = text ? (JSON.parse(text) as ApiFailure) : null;
    throw new ApiClientError(
      payload?.message ?? `Request failed with status ${response.status}.`,
      payload?.statusCode ?? response.status,
      payload?.errors ?? []
    );
  }

  return response.blob();
}
