<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::table('wave_results', function (Blueprint $table) {
            $table->unsignedSmallInteger('energy')->default(0)->after('coins');
            $table->unsignedSmallInteger('client_energy')->nullable()->after('client_coins');
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::table('wave_results', function (Blueprint $table) {
            $table->dropColumn(['energy', 'client_energy']);
        });
    }
};
