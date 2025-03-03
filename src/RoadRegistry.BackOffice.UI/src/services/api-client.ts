import Vue from "vue";
import axios, { AxiosInstance, AxiosRequestConfig, Method } from "axios";
import { AuthService } from "@/auth";
import router from "@/router";

export interface IApiClient {
  get<T = any>(
    url: string,
    query?: any,
    headers?: any,
    config?: any
  ): Promise<IApiResponse<T>>;
  delete(url: string, headers?: any): Promise<IApiResponse>;
  head(url: string, query?: any, headers?: any): Promise<IApiResponse>;
  post<T = any>(
    url: string,
    data?: any,
    headers?: any,
    config?: any,
    query?: any
  ): Promise<IApiResponse<T>>;
  put<T = any>(url: string, data?: any, headers?: any): Promise<IApiResponse<T>>;
  patch<T = any>(url: string, data?: any, headers?: any): Promise<IApiResponse<T>>;
  download(mimetype: string, filename: string, url: string, method: Method, query?: any, headers?:any): Promise<void>;
}

export interface IApiResponse<T = any> {
  data: T;
  status: number;
  statusText: string;
  headers: any;
  config: any;
  request?: any;
}

export const apiStats = Vue.observable({
  pendingRequests: 0,
});

class AxiosHttpApiClient implements IApiClient {
  private axios: AxiosInstance;

  constructor() {
    this.axios = axios.create();

    this.axios.interceptors.request.use((config: any) => {
      config.withCredentials = true;
      config.headers["x-api-key"] = AuthService.getApiKey();
      return config;
    });

    this.axios.interceptors.request.use(
      (config) => {
        apiStats.pendingRequests++;
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    this.axios.interceptors.response.use(
      (response) => {
        apiStats.pendingRequests--;
        return response;
      },
      (error) => {
        console.info(error)
        if(error.response.status == 403 || error.response.status == 401) {
          router.push("/login?redirect=" + router.currentRoute.fullPath)
        }
        apiStats.pendingRequests--;
        return Promise.reject(error);
      }
    );
  }

  public async get<T = any>(
    url: string,
    query?: any,
    headers?: any,
    config?: any
  ): Promise<IApiResponse<T>> {
    return await this.axios.get<T>(
      url,
      Object.assign({}, { params: query, headers }, config)
    );
  }
  public async download(
    mimetype: string,
    filename: string,
    url: string,
    method: Method = "GET",
    query?: any,
    headers?:any)
  {
      const response = await this.axios({
        url,
        headers,
        method,
        params: query,
        responseType: 'blob'
      });
      const blob = new Blob([response.data], {type:mimetype});
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.download = filename;
      link.click();
  }
  public async postUpload<T = any>(
    formData: FormData, url: string, config?: any, query?: any, headers?:any)
  {
      return await await this.axios.post<T>(url,formData, Object.assign({}, { params: query, headers }, config))
  }
  public async delete(url: string, headers?: any): Promise<IApiResponse> {
    return await this.axios.delete(url, { headers });
  }
  public async head(url: string, query?: any, headers?: any): Promise<IApiResponse> {
    return await this.axios.head(url, { headers });
  }
  public async post<T = any>(
    url: string,
    data?: any,
    headers?: any,
    config?: any,
    query?: any
  ): Promise<IApiResponse<T>> {
    return await this.axios.post<T>(
      url,
      data,
      Object.assign({}, { params: query, headers }, config)
    );
  }
  public async put<T = any>(
    url: string,
    data?: any,
    headers?: any
  ): Promise<IApiResponse<T>> {
    return await this.axios.put<T>(url, data, { headers });
  }
  public async patch<T = any>(
    url: string,
    data?: any,
    headers?: any
  ): Promise<IApiResponse<T>> {
    return await this.axios.patch<T>(url, data, { headers });
  }
}

export const apiClient = new AxiosHttpApiClient() as IApiClient;
export default apiClient;