// Parquet & Delta Lake Integration for Ultimate Data Tool
let parquetWasm = null;
let duckdbConnection = null;

window.initializeParquetWasm = async function() {
    try {
        // Initialize Parquet-WASM
        const parquetModule = await import('./parquet-wasm.js');
        parquetWasm = parquetModule;
        console.log('Parquet-WASM initialized successfully');
        return true;
    } catch (error) {
        console.error('Failed to initialize Parquet-WASM:', error);
        return false;
    }
};

window.initializeDuckDB = async function() {
    try {
        // Initialize DuckDB-WASM (using CDN fallback for now)
        const JSDELIVR_BUNDLES = {
            mvp: {
                mainModule: 'https://cdn.jsdelivr.net/npm/@duckdb/duckdb-wasm@1.31.0/dist/duckdb-mvp.wasm',
                mainWorker: 'https://cdn.jsdelivr.net/npm/@duckdb/duckdb-wasm@1.31.0/dist/duckdb-browser-mvp.worker.js'
            }
        };

        const bundle = await DuckDBBundle.selectBundle(JSDELIVR_BUNDLES);
        const worker = new Worker(bundle.mainWorker);
        const logger = new DuckDBConsoleLogger();
        const db = new DuckDBAsync(logger, worker);
        
        await db.instantiate(bundle.mainModule);
        duckdbConnection = await db.connect();
        
        console.log('DuckDB-WASM initialized successfully');
        return true;
    } catch (error) {
        console.error('Failed to initialize DuckDB-WASM:', error);
        return false;
    }
};

window.readParquetMetadata = async function(fileName, fileData) {
    try {
        if (!parquetWasm) {
            throw new Error('Parquet-WASM not initialized');
        }

        // Convert byte array to Uint8Array
        const uint8Array = new Uint8Array(fileData);
        
        // Read metadata using parquet-wasm
        const metadata = parquetWasm.readMetadata(uint8Array);
        
        const result = {
            version: metadata.version() || 'Unknown',
            numRows: Number(metadata.numRows() || 0),
            fileSize: uint8Array.length,
            numRowGroups: metadata.numRowGroups() || 0,
            numColumns: 0,
            createdBy: metadata.createdBy() || 'Unknown',
            keyValueMetadata: {},
            rowGroups: [],
            columns: []
        };

        // Extract schema information
        const schema = metadata.schema();
        if (schema) {
            result.numColumns = schema.numColumns() || 0;
            
            // Extract column metadata
            for (let i = 0; i < result.numColumns; i++) {
                const column = schema.column(i);
                result.columns.push({
                    name: column.name() || `column_${i}`,
                    physicalType: column.physicalType() || 'UNKNOWN',
                    logicalType: column.logicalType()?.toString() || 'NONE',
                    isRepeated: column.repetitionType() === 'REPEATED',
                    isOptional: column.repetitionType() === 'OPTIONAL',
                    maxDefinitionLevel: column.maxDefinitionLevel() || 0,
                    maxRepetitionLevel: column.maxRepetitionLevel() || 0,
                    totalSize: 0,
                    defaultCompression: 'UNCOMPRESSED'
                });
            }
        }

        // Extract row group information
        for (let i = 0; i < result.numRowGroups; i++) {
            const rowGroup = metadata.rowGroup(i);
            const rowGroupInfo = {
                index: i,
                numRows: Number(rowGroup.numRows() || 0),
                totalByteSize: Number(rowGroup.totalByteSize() || 0),
                columns: []
            };

            // Extract column chunks for this row group
            for (let j = 0; j < result.numColumns; j++) {
                const columnChunk = rowGroup.columnChunk(j);
                rowGroupInfo.columns.push({
                    path: columnChunk.filePath() || `column_${j}`,
                    type: columnChunk.type()?.toString() || 'UNKNOWN',
                    encoding: 'PLAIN', // Default, would need actual encoding info
                    compression: columnChunk.compressionType()?.toString() || 'UNCOMPRESSED',
                    uncompressedSize: Number(columnChunk.totalUncompressedSize() || 0),
                    compressedSize: Number(columnChunk.totalCompressedSize() || 0),
                    minValue: null,
                    maxValue: null,
                    nullCount: null,
                    distinctCount: null
                });
            }
            
            result.rowGroups.push(rowGroupInfo);
        }

        return JSON.stringify(result);
    } catch (error) {
        console.error('Error reading Parquet metadata:', error);
        throw error;
    }
};

window.readParquetPreview = async function(fileName, fileData, maxRows = 100) {
    try {
        if (!parquetWasm) {
            throw new Error('Parquet-WASM not initialized');
        }

        const uint8Array = new Uint8Array(fileData);
        
        // Read the Parquet file into Arrow table
        const arrowTable = parquetWasm.readParquet(uint8Array);
        
        // Convert to JavaScript objects for preview
        const schema = arrowTable.schema;
        const columns = [];
        
        // Extract schema information
        for (let i = 0; i < schema.fields.length; i++) {
            const field = schema.fields[i];
            columns.push({
                name: field.name,
                type: field.type.toString(),
                nullable: field.nullable,
                children: [] // TODO: Handle nested types
            });
        }

        // Extract data rows
        const rows = [];
        const actualMaxRows = Math.min(maxRows, arrowTable.numRows);
        
        for (let i = 0; i < actualMaxRows; i++) {
            const row = {};
            for (let j = 0; j < schema.fields.length; j++) {
                const field = schema.fields[j];
                const column = arrowTable.getChild(j);
                try {
                    row[field.name] = column.get(i);
                } catch (e) {
                    row[field.name] = null;
                }
            }
            rows.push(row);
        }

        const result = {
            rows: rows,
            totalRows: arrowTable.numRows,
            hasMore: arrowTable.numRows > maxRows,
            schema: columns
        };

        return JSON.stringify(result);
    } catch (error) {
        console.error('Error reading Parquet preview:', error);
        throw error;
    }
};

window.getParquetSchema = async function(fileName, fileData) {
    try {
        if (!parquetWasm) {
            throw new Error('Parquet-WASM not initialized');
        }

        const uint8Array = new Uint8Array(fileData);
        const arrowTable = parquetWasm.readParquet(uint8Array);
        const schema = arrowTable.schema;
        
        const columns = [];
        let maxDepth = 0;
        let hasNestedTypes = false;

        function processField(field, depth = 0) {
            maxDepth = Math.max(maxDepth, depth);
            
            const column = {
                name: field.name,
                type: field.type.toString(),
                nullable: field.nullable,
                children: []
            };

            // Check for nested types
            if (field.type.children && field.type.children.length > 0) {
                hasNestedTypes = true;
                for (const child of field.type.children) {
                    column.children.push(processField(child, depth + 1));
                }
            }

            return column;
        }

        for (const field of schema.fields) {
            columns.push(processField(field));
        }

        const result = {
            name: fileName,
            columns: columns,
            maxDepth: maxDepth,
            hasNestedTypes: hasNestedTypes
        };

        return JSON.stringify(result);
    } catch (error) {
        console.error('Error getting Parquet schema:', error);
        throw error;
    }
};

window.getParquetStatistics = async function(fileName, fileData) {
    try {
        if (!parquetWasm) {
            throw new Error('Parquet-WASM not initialized');
        }

        const uint8Array = new Uint8Array(fileData);
        const metadata = parquetWasm.readMetadata(uint8Array);
        const arrowTable = parquetWasm.readParquet(uint8Array);
        
        const columnStatistics = {};
        const compressionCodecUsage = {};
        const encodingUsage = {};
        
        // Calculate basic statistics
        let totalUncompressedSize = 0;
        let totalCompressedSize = 0;

        // Process row groups for statistics
        for (let i = 0; i < metadata.numRowGroups(); i++) {
            const rowGroup = metadata.rowGroup(i);
            
            for (let j = 0; j < metadata.schema().numColumns(); j++) {
                const column = metadata.schema().column(j);
                const columnChunk = rowGroup.columnChunk(j);
                const columnName = column.name();
                
                if (!columnStatistics[columnName]) {
                    columnStatistics[columnName] = {
                        columnName: columnName,
                        nonNullCount: 0,
                        nullCount: 0,
                        minValue: null,
                        maxValue: null,
                        distinctCount: 0,
                        compressionRatio: 0,
                        uncompressedSize: 0,
                        compressedSize: 0
                    };
                }

                const stats = columnStatistics[columnName];
                const uncompressed = Number(columnChunk.totalUncompressedSize() || 0);
                const compressed = Number(columnChunk.totalCompressedSize() || 0);
                
                stats.uncompressedSize += uncompressed;
                stats.compressedSize += compressed;
                
                totalUncompressedSize += uncompressed;
                totalCompressedSize += compressed;

                // Track compression codec usage
                const compression = columnChunk.compressionType()?.toString() || 'UNCOMPRESSED';
                compressionCodecUsage[compression] = (compressionCodecUsage[compression] || 0) + 1;

                // Track encoding usage (simplified)
                encodingUsage['PLAIN'] = (encodingUsage['PLAIN'] || 0) + 1;
            }
        }

        // Calculate compression ratios
        for (const stats of Object.values(columnStatistics)) {
            if (stats.compressedSize > 0) {
                stats.compressionRatio = stats.uncompressedSize / stats.compressedSize;
            }
        }

        const result = {
            totalRows: Number(metadata.numRows() || 0),
            totalSize: uint8Array.length,
            columnStatistics: columnStatistics,
            compressionRatio: totalCompressedSize > 0 ? totalUncompressedSize / totalCompressedSize : 1.0,
            compressionCodecUsage: compressionCodecUsage,
            encodingUsage: encodingUsage
        };

        return JSON.stringify(result);
    } catch (error) {
        console.error('Error getting Parquet statistics:', error);
        throw error;
    }
};

window.queryParquetData = async function(fileName, fileData, sqlQuery) {
    try {
        if (!duckdbConnection) {
            await window.initializeDuckDB();
        }

        // Register the Parquet file as a table in DuckDB
        const uint8Array = new Uint8Array(fileData);
        const tableName = 'parquet_table_' + Date.now();
        
        // Use DuckDB's ability to read Parquet data directly
        await duckdbConnection.query(`
            CREATE TEMPORARY TABLE ${tableName} AS 
            SELECT * FROM read_parquet_from_binary($1::BLOB)
        `, [uint8Array]);
        
        // Execute the user's query
        const result = await duckdbConnection.query(sqlQuery.replace(/FROM\s+\w+/gi, `FROM ${tableName}`));
        
        // Convert result to JSON
        const rows = [];
        while (true) {
            const row = await result.fetchRow();
            if (!row) break;
            
            const rowData = {};
            for (let i = 0; i < result.schema.fields.length; i++) {
                const field = result.schema.fields[i];
                rowData[field.name] = row[i];
            }
            rows.push(rowData);
        }
        
        return JSON.stringify({
            rows: rows,
            totalRows: rows.length,
            schema: result.schema.fields.map(f => ({
                name: f.name,
                type: f.type.toString(),
                nullable: f.nullable
            }))
        });
    } catch (error) {
        console.error('Error querying Parquet data:', error);
        throw error;
    }
};

window.convertParquetToJson = async function(fileName, fileData) {
    try {
        if (!parquetWasm) {
            throw new Error('Parquet-WASM not initialized');
        }

        const uint8Array = new Uint8Array(fileData);
        const arrowTable = parquetWasm.readParquet(uint8Array);
        
        const rows = [];
        const schema = arrowTable.schema;
        
        for (let i = 0; i < arrowTable.numRows; i++) {
            const row = {};
            for (let j = 0; j < schema.fields.length; j++) {
                const field = schema.fields[j];
                const column = arrowTable.getChild(j);
                try {
                    row[field.name] = column.get(i);
                } catch (e) {
                    row[field.name] = null;
                }
            }
            rows.push(row);
        }

        const jsonString = JSON.stringify(rows, null, 2);
        return new TextEncoder().encode(jsonString);
    } catch (error) {
        console.error('Error converting Parquet to JSON:', error);
        throw error;
    }
};

window.convertParquetToCsv = async function(fileName, fileData, options) {
    try {
        if (!parquetWasm) {
            throw new Error('Parquet-WASM not initialized');
        }

        const uint8Array = new Uint8Array(fileData);
        const arrowTable = parquetWasm.readParquet(uint8Array);
        
        const schema = arrowTable.schema;
        const delimiter = options.delimiter || ',';
        const includeHeaders = options.includeHeaders !== false;
        
        let csvContent = '';
        
        // Add headers
        if (includeHeaders) {
            const headers = schema.fields.map(f => `"${f.name}"`);
            csvContent += headers.join(delimiter) + '\n';
        }
        
        // Add data rows
        for (let i = 0; i < arrowTable.numRows; i++) {
            const row = [];
            for (let j = 0; j < schema.fields.length; j++) {
                const column = arrowTable.getChild(j);
                let value;
                try {
                    value = column.get(i);
                    if (value === null || value === undefined) {
                        value = '';
                    } else if (typeof value === 'string') {
                        value = `"${value.replace(/"/g, '""')}"`;
                    } else {
                        value = String(value);
                    }
                } catch (e) {
                    value = '';
                }
                row.push(value);
            }
            csvContent += row.join(delimiter) + '\n';
        }

        return new TextEncoder().encode(csvContent);
    } catch (error) {
        console.error('Error converting Parquet to CSV:', error);
        throw error;
    }
};

// Delta Lake functions (simplified implementations)
window.initializeDeltaLake = async function() {
    try {
        // For now, we'll use basic file system operations
        // In a full implementation, this would initialize Delta-RS WASM bindings
        console.log('Delta Lake initialized (basic implementation)');
        return true;
    } catch (error) {
        console.error('Failed to initialize Delta Lake:', error);
        return false;
    }
};

window.getDeltaTableMetadata = async function(tablePath) {
    try {
        // This would read the _delta_log directory and parse metadata
        // For now, return a mock structure
        const result = {
            id: 'mock-table-id',
            name: tablePath.split('/').pop() || 'unknown',
            description: 'Mock Delta table for development',
            schema: {
                columns: [],
                type: 'struct'
            },
            partitionColumns: [],
            configuration: {},
            version: 0,
            createdTime: new Date().toISOString(),
            lastModified: new Date().toISOString(),
            minReaderVersion: 1,
            minWriterVersion: 1,
            readerFeatures: [],
            writerFeatures: []
        };

        return JSON.stringify(result);
    } catch (error) {
        console.error('Error getting Delta table metadata:', error);
        throw error;
    }
};

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        initializeParquetWasm,
        initializeDuckDB,
        readParquetMetadata,
        readParquetPreview,
        getParquetSchema,
        getParquetStatistics,
        queryParquetData,
        convertParquetToJson,
        convertParquetToCsv
    };
}

console.log('Parquet & Delta Lake integration loaded');