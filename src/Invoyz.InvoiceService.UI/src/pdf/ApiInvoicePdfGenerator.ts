import type { HttpClient } from "@/core/http/HttpClient";
import type { InvoicePdfGenerator } from "./InvoicePdfGenerator";
import type { Invoice } from "@/domain/models";
import { ApiError } from "@/core/errors/ApiError";
import { downloadBlob } from "@/core/download";

export class ApiInvoicePdfGenerator implements InvoicePdfGenerator{
    readonly isAvailable = true;

    constructor(private readonly http: HttpClient, private readonly basePath = '/Documents'){

    }

    async generate(invoice: Invoice): Promise<void> {
        const { blob, fileName} = await this.http.getBinary({
            url: `${this.basePath}/${encodeURIComponent(invoice.id)}`,
        })

        if(blob.size == 0)
        {
            throw new ApiError('unexpected','The API returned an empty document.')
        }

        downloadBlob(blob, fileName ?? `${invoice.id}`)
    }
}